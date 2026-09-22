from dataclasses import dataclass, field
from typing import Any

# Package of information used for calling services, sent from the model
@dataclass
class ServiceCall:
    service_name: str
    arguments: dict[str, Any]
    returns: Any

# Package of information that describes the result of a requested service, sent from the execution layer
@dataclass
class ServiceResult:
    service_name: str
    service_success: bool
    service_result: Any = None
    service_error: str | None = None

# Package of information sent to the model each turn
@dataclass
class ModelInput:
    # Original task requested by the user
    user_task: str

    # User's latest response, if the model asked a question
    user_response: str = ""

    # Results from services that were executed
    service_results: list[ServiceResult] = field(default_factory=list)

    # Summary stored for continuity
    summary: str = ""

# Package of information sent by the model each turn
@dataclass
class ModelResponse:
    # Always shown to the user
    model_response: str

    # Optional service calls
    requested_services: list[ServiceCall] = field(default_factory=list)

    # Always explicitly set by the model
    task_complete: bool = False

    # Updated continuity summary
    summary: str = ""