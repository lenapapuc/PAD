from flask import Flask, request, jsonify, make_response
from flask_caching import Cache
import requests
import logging

app = Flask(__name__)

cache = Cache(app, config={'CACHE_TYPE': 'simple'})

# Add logging configuration
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)



# Define multiple instances of the listing service
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
    booking_data = request.json
    instance = get_next_booking_service()
    response = requests.post(f"{instance}/bookings/book", json=booking_data, verify=False)
    logger.info(f"Data processed by {instance}")
    return (response.text, response.status_code)


@app.route('/bookings/<uuid:id>', methods=['GET'])
def get_booking(id):
    instance = get_next_booking_service()
    response = requests.get(f"{instance}bookings/{id}", verify=False)
    logger.info(f"Data for city '{id}' fetched from {instance} and cached.")
    return (response.text, response.status_code)

# Routes for the Listing microservice
@app.route('/listings/create', methods=['POST'])
def create_listing():
    instance = get_next_listing_service()
    listing_data = request.json
    response = requests.post(f"{instance}/listings/create", json=listing_data, verify=False)
    logger.info(f"Data processed by {instance}")
    return (response.text, response.status_code)

@app.route('/listings/city/<city_name>', methods=['GET'])
def get_listings_in_city(city_name):
    # Define a cache key for the specific city_name
    cache_key = f"listings_{city_name}"

    # Try to fetch the data from the cache
    cached_data = cache.get(cache_key)
    if cached_data is not None:
        # Data was found in the cache, log it
        logger.info(f"Data for city '{city_name}' fetched from cache.")
        return cached_data
    instance = get_next_listing_service()
    # Data was not found in the cache, fetch it from the Listing microservice
    response = requests.get(f"{instance}/listings/city/{city_name}", verify=False)
    app.logger.info(f"Response status code: {response.status_code}")
    app.logger.info(f"Response content: {response.text}")
    if response.status_code == 200:

        cache.set(cache_key, (response.text, response.status_code), timeout=300)
        logger.info(f"Data for city '{city_name}' fetched from {instance} and cached.")
        logger.info(f"Data for city '{city_name}' fetched from the service and cached.")

    # Return the response to the client
    return (response.text, response.status_code)

@app.route('/listings/<uuid:id>', methods=['GET'])
def get_listing(id):
    instance = get_next_listing_service()
    response = requests.get(f"{instance}/listings/{id}", verify=False)
    logger.info(f"Data for city '{id}' fetched from {instance} and cached.")
    return (response.text, response.status_code)

@app.route('/listings/update/<uuid:id>', methods=['PATCH'])
def update_listing(id):
    instance = get_next_listing_service()
    listing_data = request.json
    response = requests.patch(f"{instance}/listings/update/{id}", json=listing_data, verify=False)
    logger.info(f"Data processed by {instance}")
    return (response.text, response.status_code)

@app.route('/listings/delete/<uuid:id>', methods=['DELETE'])
def delete_listing(id):
    instance = get_next_listing_service()
    response = requests.delete(f"{instance}/listings/delete/{id}", verify=False)
    logger.info(f"Data processed by {instance}")
    return (response.text, response.status_code)

def get_next_listing_service():
    global listing_service_index
    service_instance = listing_service_instances[listing_service_index]
    listing_service_index = (listing_service_index + 1) % len(listing_service_instances)
    return service_instance

def get_next_booking_service():
    global booking_service_index
    service_instance = booking_service_instances[booking_service_index]
    booking_service_index = (booking_service_index + 1) % len(booking_service_instances)
    return service_instance

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)  # Run the gateway on port 5000

