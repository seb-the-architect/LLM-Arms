from openai import OpenAI

from Communication_Definitions import *

from LMStudio_Configs.Gaia.gaia_config import gaia_config
from LMStudio_Communication.openai_communication import LMStudioClient

# Create connection instance
LMStudioClientInstance = LMStudioClient(gaia_config)
# Load desired model
print(LMStudioClientInstance.load_model())

# Run forever
while True:
    # Until the task is complete...
    task_complete = False
    while not task_complete:
        # Get desired user task
        user_task = input("Task: ")
        # Package into output struct
        model_input = ModelInput(user_task)
        print(model_input)
        input()