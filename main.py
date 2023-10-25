from flask import Flask, request, jsonify, make_response
from flask_caching import Cache
import requests
import logging

app = Flask(__name__)

cache = Cache(app, config={'CACHE_TYPE': 'simple'})

# Add logging configuration
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Define the base URLs for your microservices
booking_service_base_url = "http://localhost:5261"  # Update with your Booking microservice URL
listing_service_base_url = "http://localhost:5114"  # Update with your Listing microservice URL

# Routes for the Booking microservice
@app.route('/bookings/create', methods=['POST'])
def create_booking():
    booking_data = request.json
    response = requests.post(f"{booking_service_base_url}/bookings/book", json=booking_data)
    return (response.text, response.status_code)


@app.route('/bookings/<uuid:id>', methods=['GET'])
def get_booking(id):
    response = requests.get(f"{booking_service_base_url}/bookings/{id}")
    return (response.text, response.status_code)

# Routes for the Listing microservice
@app.route('/listings/create', methods=['POST'])
def create_listing():
    listing_data = request.json
    response = requests.post(f"{listing_service_base_url}/listings/create", json=listing_data)
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

    # Data was not found in the cache, fetch it from the Listing microservice
    response = requests.get(f"{listing_service_base_url}/listings/city/{city_name}")

    if response.status_code == 200:
        # Cache the response for future use with a time limit (e.g., 300 seconds)
        cache.set(cache_key, (response.text, response.status_code), timeout=300)
        logger.info(f"Data for city '{city_name}' fetched from the service and cached.")

    # Return the response to the client
    return (response.text, response.status_code)

@app.route('/listings/<uuid:id>', methods=['GET'])
def get_listing(id):
    response = requests.get(f"{listing_service_base_url}/listings/{id}")
    return (response.text, response.status_code)

@app.route('/listings/update/<uuid:id>', methods=['PATCH'])
def update_listing(id):
    listing_data = request.json
    response = requests.patch(f"{listing_service_base_url}/listings/update/{id}", json=listing_data)
    return (response.text, response.status_code)

@app.route('/listings/delete/<uuid:id>', methods=['DELETE'])
def delete_listing(id):
    response = requests.delete(f"{listing_service_base_url}/listings/delete/{id}")
    return (response.text, response.status_code)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)  # Run the gateway on port 5000

