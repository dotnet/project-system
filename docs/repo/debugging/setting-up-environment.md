# Setting up your Debugging Environment

## Debugger Settings

The day-to-day job of a developer that works on Visual Studio involves debugging and inspecting code outside of this repository. For best results, you'll want to flip some of the defaults so that you can can step into (Microsoft-employees only) other Visual Studio, framework and Windows code and inspect locals, fields, etc.

![image](https://user-images.githubusercontent.com/1103906/44320403-3fa23200-a485-11e8-9baa-b743cb96948d.png)

![image](https://user-images.githubusercontent.com/1103906/44320478-8a23ae80-a485-11e8-9426-0b7906093e9a.png)

![image](https://user-images.githubusercontent.com/1103906/44320534-e25ab080-a485-11e8-885b-811800d20684.png)

## Launch Settings

If you need to debug into native code (for example, msenv.dll, where the solution code lives), you can change your launch settings from ProjectSystem.sln to launch with the native debugger engine:

![image](https://user-images.githubusercontent.com/1103906/44320680-e2a77b80-a486-11e8-9887-8e5a44a4a26c.png)

If you are having troubling inspecting variables due to optimizations (assuming __Suppress JIT optimization on module load__ above is checked), you can also try bypassing NGEN images which should improve your debugging experience.
