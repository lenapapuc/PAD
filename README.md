# PAD

 # Gateway

docker run -p 5000:5000 elenapapuc/pad_1:gateway

### Endpoints:

 GET /listings/city/{cityName}
          
GET /listings/{id}
          
GET /bookings/{id}
          
POST /bookings/create

 {
    "ListingId" : "E094C3EE-F40A-4100-8D67-08DBD251FA4A",
    "UserId" : "123",
    "PaymentMethod" : "Card",
    "StartDate" : "2023-10-24",
    "EndDate" : "2023-10-24"
}

POST /listings/create

{
    "Name": "Example Listing",
    "Description": "This is an example listing description.",
    "User_Id": 1, 
    "Location": "Example City",
    "Price": 100.00,
    "Country": "Example Country",
    "Amenities": "This is an amenity",
    "Availability": [
        {
            "Date": "2023-10-30",
            "Available": true,
            "Price": 110.00
        },
        {
            "Date": "2023-11-05",
            "Available": false,
            "Price": null 
        }
    ]
}

PATCH /listings/update/{id}

    "Availability": [
        {
            "Date": "2023-10-30",
            "Available": true,
            "Price": 110.00
        },
        {
            "Date": "2023-11-05",
            "Available": true,
            "Price": null 
        }
    ]

DELETE /listings/delete/{id}


# Microservice Listings

docker run -p 5114:80 elenapapuc/pad_1:listings

# Microservice Bookings 

docker run -p 5261:80 elenapapuc/pad_1:booking


! endpoints for listings and bookings correspond to the ones in the gateway except bookings/create becomes bookings/book

