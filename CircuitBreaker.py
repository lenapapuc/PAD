from pybreaker import CircuitBreaker, CircuitBreakerListener
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class CustomCircuitBreakerListener(CircuitBreakerListener):
    def __init__(self, reroute_threshold=3):
        super().__init__()
        self.reroute_count = 0
        self.reroute_threshold = reroute_threshold

    def reset(self):
        self.reroute_count = 0

    def on_failure(self, cb, exc):
        if isinstance(exc, Exception):
            self.reroute_count += 1


class RerouteException(Exception):
    pass
