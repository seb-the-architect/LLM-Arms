import subprocess
import time
from dataclasses import dataclass
from typing import Any

import requests
from openai import OpenAI

SERVER_URL: str = "http://localhost:1234"
OPENAI_BASE_URL: str = "http://localhost:1234/v1"
API_KEY: str = "lm-studio"

class LMStudioConfig:
    def __init__(self, model_name: str, system_prompt_dir: str, context_length: int, temperature: float):
        self.model_name = model_name
        self.system_prompt_dir = system_prompt_dir
        self.context_length = context_length
        self.temperature = temperature

class LMStudioClient:
    def __init__(self, config: LMStudioConfig):
        self.config = config

        self.client = OpenAI(
            base_url=OPENAI_BASE_URL,
            api_key=API_KEY
        )

    def start_server(self) -> None:
        subprocess.run(
            [
                "lms",
                "server",
                "start",
                "--port",
                "1234"
            ],
            check=True
        )

    def wait_until_server_ready(self, timeout_seconds: int = 30) -> None:
        start_time = time.time()

        while time.time() - start_time < timeout_seconds:
            try:
                response = requests.get(
                    f"{OPENAI_BASE_URL}/models",
                    timeout=2
                )

                if response.status_code == 200:
                    return

            except requests.RequestException:
                pass

            time.sleep(1)

        raise RuntimeError("LM Studio server did not become ready.")

    def load_model(self) -> dict[str, Any]:
        response = requests.post(
            f"{SERVER_URL}/api/v1/models/load",
            json={
                "model": self.config.model_name,
                "context_length": self.config.context_length,
                "echo_load_config": True
            }
        )

        response.raise_for_status()
        return response.json()

    def list_models(self) -> list[str]:
        models = self.client.models.list()
        return [model.id for model in models.data]

    def send(self, system_prompt: str, user_prompt: str) -> str:
        completion = self.client.chat.completions.create(
            model=self.config.model_name,
            messages=[
                {
                    "role": "system",
                    "content": system_prompt
                },
                {
                    "role": "user",
                    "content": user_prompt
                }
            ],
            temperature=self.config.temperature
        )

        return completion.choices[0].message.content