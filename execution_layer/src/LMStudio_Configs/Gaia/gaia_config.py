from pathlib import Path
from dataclasses import dataclass

from LMStudio_Communication.openai_communication import LMStudioConfig

@dataclass
class Gaia_Config:
    model_name: str = "openai-gpt-oss-20b"
    system_prompt_dir = Path("system_prompt.json").resolve()
    context_length: int = 36000
    temperature: float = 0.1

gaia_config = LMStudioConfig(Gaia_Config.model_name, Gaia_Config.system_prompt_dir, Gaia_Config.context_length, Gaia_Config.temperature)
