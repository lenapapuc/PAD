from flask import Flask, request, jsonify, make_response
from flask_caching import Cache
import requests
import logging
from pybreaker import CircuitBreaker, CircuitBreakerListener, CircuitBreakerError, CircuitClosedState
from requests import RequestException

from CircuitBreaker import CustomCircuitBreakerListener, RerouteException

# Create circuit breakers
listing_circuit_breaker1 = CircuitBreaker(fail_max=2, reset_timeout=30, listeners=[CustomCircuitBreakerListener()])
listing_circuit_breaker2 = CircuitBreaker(fail_max=2, reset_timeout=30, listeners=[CustomCircuitBreakerListener()])
listing_circuit_breaker3 = CircuitBreaker(fail_max=2, reset_timeout=30, listeners=[CustomCircuitBreakerListener()])

booking_circuit_breaker = CircuitBreaker(fail_max=3, reset_timeout=30, listeners=[CustomCircuitBreakerListener()])
app = Flask(__name__)

cache = Cache(app, config={'CACHE_TYPE': 'simple'})

# Add logging configuration
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)



listing_circuit_breakers = {
    "http://localhost:7126": listing_circuit_breaker1,
    "http://localhost:7127": listing_circuit_breaker2,
    "http://localhost:7128": listing_circuit_breaker3
}

listing_service_instances = [
    "http://localhost:7126",
    "http://localhost:7127",
    "http://localhost:7128"
]


booking_service_instances = [
    "http://localhost:5100",
    "http://localhost:5101",
    "http://localhost:5102",
]

# Initialize an index to keep track of the next instance to use
listing_service_index = 0
booking_service_index = 0


# Routes for the Booking microservice
@app.route('/bookings/create', methods=['POST'])
def create_booking():
    try:
        booking_data = request.json
        instance = get_next_booking_service()
        response = requests.post(f"{instance}/bookings/book", json=booking_data, verify=False)
        logger.info(f"Data processed by {instance}")
        return (response.text, response.status_code)
    except Exception as e:
        logger.error(f"An unexpected error occurred: {e}")
        return "Service temporarily unavailable", 503


@app.route('/bookings/<uuid:id>', methods=['GET'])
def get_booking(id):
    try:
        instance = get_next_booking_service()
        response = requests.get(f"{instance}bookings/{id}", verify=False)
        logger.info(f"Data for city '{id}' fetched from {instance} and cached.")
        return (response.text, response.status_code)
    except Exception as e:
        logger.error(f"An unexpected error occurred: {e}")
        return "Service temporarily unavailable", 503


# Routes for the Listing microservice

@app.route('/listings/create', methods=['POST'])
def create_listing():
    try:
        instance = get_next_listing_service()
        listing_data = request.json
        if instance not in listing_circuit_breakers:
            listing_circuit_breakers[instance] = CircuitBreaker(fail_max=2, reset_timeout=30,
                                                                listeners=[CustomCircuitBreakerListener()])

        response = listing_circuit_breakers[instance].call(lambda:requests.post(f"{instance}/listings/create", json=listing_data, verify=False))
        logger.info(f"Data processed by {instance}")
        return (response.text, response.status_code)
    except CircuitBreakerError as e:
        logger.error(f"Circuit breaker opened: {e}")
        return "Circuit Opened", 503
    except Exception as e:
        logger.error(f"An unexpected error occurred: {e}")
        return "Service temporarily unavailable", 503
    pass


@app.route('/listings/city/<city_name>', methods=['GET'])
def get_listings_in_city(city_name):
    try:
        # Define a cache key for the specific city_name
        cache_key = f"listings_{city_name}"

        # Try to fetch the data from the cache
        cached_data = cache.get(cache_key)
        if cached_data is not None:
            # Data was found in the cache, log it
            logger.info(f"Data for city '{city_name}' fetched from cache.")
            return cached_data
        instance = get_next_listing_service()
        # Check if the circuit breaker is closed (not tripped)

        # Data was not found in the cache, fetch it from the Listing microservice
        response = listing_circuit_breakers[instance].call(lambda: requests.get(f"{instance}/listings/city/{city_name}", verify=False))

        app.logger.info(f"Response status code: {response.status_code}")
        app.logger.info(f"Response content: {response.text}")
        if response.status_code == 200:
            cache.set(cache_key, (response.text, response.status_code), timeout=300)
            logger.info(f"Data for city '{city_name}' fetched from {instance} and cached.")
            logger.info(f"Data for city '{city_name}' fetched from the service and cached.")

            # Return the response to the client
        return (response.text, response.status_code)

    except CircuitBreakerError as e:
        logger.error(f"Circuit breaker opened: {e}")
        return get_listings_in_city(city_name)
    except Exception as e:
        logger.error(f"An unexpected error occurred: {e}")
        return get_listings_in_city(city_name)



@app.route('/listings/<uuid:id>', methods=['GET'])
def get_listing(id):
    try:
        instance = get_next_listing_service()
        response = listing_circuit_breakers[instance].call(lambda:requests.get(f"{instance}/listings/{id}", verify=False))
        logger.info(f"Data for city '{id}' fetched from {instance} and cached.")
        return (response.text, response.status_code)
    except CircuitBreakerError as e:
        logger.error(f"Circuit breaker opened: {e}")
        return "Circuit Opened", 503
    except Exception as e:
        logger.error(f"An unexpected error occurred: {e}")
        return "Service temporarily unavailable", 503
    pass


@app.route('/listings/update/<uuid:id>', methods=['PATCH'])
def update_listing(id):
    try:
        instance = get_next_listing_service()
        listing_data = request.json
        response = listing_circuit_breakers[instance].call(lambda:requests.patch(f"{instance}/listings/update/{id}", json=listing_data, verify=False))
        logger.info(f"Data processed by {instance}")
        return (response.text, response.status_code)
    except CircuitBreakerError as e:
        logger.error(f"Circuit breaker opened: {e}")
        return "Circuit Opened", 503
    except Exception as e:
        logger.error(f"An unexpected error occurred: {e}")
        return "Service temporarily unavailable", 503
    pass


@app.route('/listings/delete/<uuid:id>', methods=['DELETE'])
def delete_listing(id):
    try:
        instance = get_next_listing_service()
        response = listing_circuit_breakers[instance].call(lambda:requests.delete(f"{instance}/listings/delete/{id}", verify=False))
        logger.info(f"Data processed by {instance}")
        return (response.text, response.status_code)
    except CircuitBreakerError as e:
        logger.error(f"Circuit breaker opened: {e}")
        return "Circuit Opened", 503
    except Exception as e:
        logger.error(f"An unexpected error occurred: {e}")
        return "Service temporarily unavailable", 503
    pass


def get_next_listing_service():
    global listing_service_index

    for _ in range(len(listing_service_instances)):
        service_instance = listing_service_instances[listing_service_index]
        app.logger.info(f"service is {service_instance}")
        circuit_breaker = listing_circuit_breakers.get(service_instance)
        app.logger.info(f"circuit is {type(circuit_breaker.state)}")
        # Check if the circuit breaker exists and is closed (not tripped)
        # If one request does not go through, the next time a request is made, the Round robin will
        # give it to the next instance
        if circuit_breaker is None or type(circuit_breaker.state) == CircuitClosedState:
            listing_service_index = (listing_service_index + 1) % len(listing_service_instances)
            return service_instance

        # Move to the next service instance
        listing_service_index = (listing_service_index + 1) % len(listing_service_instances)

    # If all instances are failing, you can handle it based on your requirements.
    # For example, raise a custom exception or return a specific response to the client.
    raise RerouteException("All listing service instances are failing")



def get_next_booking_service():
    global booking_service_index
    service_instance = booking_service_instances[booking_service_index]
    booking_service_index = (booking_service_index + 1) % len(booking_service_instances)
    return service_instance



if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)  # Run the gateway on port 5000
