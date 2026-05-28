# ElectricalSimulation2D

Unity 2D teaching/demo prototype for a simple electrical simulation workbench.

Open this folder with Unity 2022.3 LTS. After the project opens, choose `Tools/Electrical Demo/Build Demo Scene` from the Unity menu. The editor script will create `Assets/Scenes/Demo.unity` and starter component definitions.

First demo scope:
- Drag components from the left palette to the grid workspace.
- Click terminals to draw wires.
- Toggle switches/buttons by double clicking components.
- Click Start Simulation to light lamps, spin fans, and energize contactors when simple L/N paths are valid.
- Save and load the current drawing as JSON.

This is intentionally a teaching/demo simulator, not an industrial SPICE or PLC platform.
