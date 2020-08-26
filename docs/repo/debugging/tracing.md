# Tracing

Common Project System (CPS) writes traces messages to both a TraceSource and a circular buffer.

## Inspecting Trace While Debugging

When you build this repository under debug either within Visual Studio or via the command-line, a trace listener is hooked up to output CPS tracing to the Debug category of the Output Window. You can use this to diagnose lots of issues, such as failing rules or missing snapshots.

You can increase the verbosity of what is output to the window by changing the verbosity level in [ManagedProjectSystemPackage.DebuggingTraceListener](/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/Packaging/DebuggerTraceListener.cs).

## Inspecting Trace Within a Memory Dump

You can inspect the circular buffer within WinDbg with the following:

```
> !name2ee Microsoft.VisualStudio.ProjectSystem.dll Microsoft.VisualStudio.ProjectSystem.TraceUtilities
```
```
Module:      10c841c0
Assembly:    Microsoft.VisualStudio.ProjectSystem.dll
Token:       02000180
MethodTable: 16f88e70
EEClass:     07f85f14
Name:        Microsoft.VisualStudio.ProjectSystem.TraceUtilities
```
```
> !DumpClass /d 07f85f14
```

```
Class Name:      Microsoft.VisualStudio.ProjectSystem.TraceUtilities
mdToken:         02000180
File:            c:\program files (x86)\microsoft visual studio\preview\enterprise\common7\ide\commonextensions\microsoft\project\Microsoft.VisualStudio.ProjectSystem.dll
Parent Class:    6ded15b0
Module:          10c841c0
Method Table:    16f88e70
Vtable Slots:    4
Total Method Slots:  5
Class Attributes:    100180  Abstract, 
Transparency:        Critical
NumInstanceFields:   0
NumStaticFields:     3
      MT    Field   Offset                 Type VT     Attr    Value Name
6d69ee10  4000326      1f4 ...stics.TraceSource  0   shared   static Source
    >> Domain:Value  00b88180:0c559288 <<
6e37dfdc  4000327      1f8      System.String[]  0   shared   static CriticalTraceRotatingBuffer
    >> Domain:Value  00b88180:0c559360 <<
6e37f2d8  4000328      434         System.Int32  1   shared   static currentTraceIndex
    >> Domain:Value  00b88180:0 <<
```

```
> !DumpObj /d 0c559360; * Dump CriticalTraceRotatingBuffer
```
```
Name:        System.String[]
MethodTable: 6e37dfdc
EEClass:     6df54b80
Size:        140(0x8c) bytes
Array:       Rank 1, Number of elements 32, Type CLASS (Print Array)
Fields:
None
```
```
> !DumpArray /d 0c559360; * Print Array
```
This will output the values in the array if it can
```
Name:        System.String[]
MethodTable: 6e37dfdc
EEClass:     6df54b80
Size:        140(0x8c) bytes
Array:       Rank 1, Number of elements 32, Type CLASS
Element Methodtable: 6e37d484
[0] 11c4be4c
[1] 11c4be4c
[2] 11c4be4c
[3] null
[4] null
[5] null
[6] null
[7] null
[8] null
[9] null
[10] null
[11] null
[12] null
[13] null
[14] null
[15] null
[16] null
[17] null
[18] null
[19] null
[20] null
[21] null
[22] null
[23] null
[24] null
[25] null
[26] null
[27] null
[28] null
[29] null
[30] null
[31] null
```
> !DumpObj /d 11c4be4c
```
Name:        System.String
MethodTable: 7286eb40
EEClass:     72444a50
Size:        190(0xbe) bytes
File:        C:\WINDOWS\Microsoft.Net\assembly\GAC_32\mscorlib\v4.0_4.0.0.0__b77a5c561934e089\mscorlib.dll
String:      Generating snapshot for rule "{0}" failed because the rule was missing from the project.
Fields:
      MT    Field   Offset                 Type VT     Attr    Value Name
72870994  4000279        4         System.Int32  1 instance       88 m_stringLength
7286f588  400027a        8          System.Char  1 instance       47 m_firstChar
7286eb40  400027e       5c        System.String  0   shared   static Empty
    >> Domain:Value  01187c20:NotInit  <<
```
💡 Maybe there is a problem with a rule?
