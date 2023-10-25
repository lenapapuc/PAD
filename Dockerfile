# Use the official Python image for Windows
FROM python:3.8-windowsservercore

# Set the working directory in the container
WORKDIR /app

# Copy your Flask application files to the container
COPY . /app

# Install any necessary dependencies
RUN pip install -r requirements.txt

# Expose the port your Flask app will run on
EXPOSE 5000

# Command to run your Flask application
CMD ["python", "main.py"]
