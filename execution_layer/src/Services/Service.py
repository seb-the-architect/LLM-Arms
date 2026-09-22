from abc import ABC, abstractmethod
from typing import Any

# Service abstract base class
class Service(ABC):
    name: str
    parameters: dict[str, Any]
    description: str
    requires_permission: bool
    returns: Any

    @abstractmethod
    def run(self, *args: Any, **kwargs: Any) -> Any:
        pass