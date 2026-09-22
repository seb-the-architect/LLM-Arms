from pathlib import Path
from typing import Any

from Services.Service import Service

class WriteFileService(Service):
    name = "io.write_file"
    description = "Write text content to a file."
    requires_permission = True
    returns: None

    base_dir = Path("workspace").resolve()

    parameters = {
        "required": ["file_name", "file_content"],
        "properties": {
            "file_name": {
                "type": "string",
                "description": "The name of the file."
            },
            "file_content": {
                "type": "string",
                "description": "Text content to write to the file."
            }
        }
    }

    def run(self, file_name: str, file_content: str) -> dict[str, Any]:
        file_path = (self.base_dir / file_name).resolve()

        if not file_path.is_relative_to(self.base_dir):
            raise ValueError("File path escapes workspace")

        file_path.parent.mkdir(parents=True, exist_ok=True)
        file_path.write_text(file_content, encoding="utf-8")

        return {
            "path": str(file_path),
            "message": f"Wrote file: {file_name}"
        }


write_file_service = WriteFileService()