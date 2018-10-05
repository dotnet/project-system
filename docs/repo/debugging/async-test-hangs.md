# Debugging Async Test Hangs

(Adapted from an internal document originally written by @jaredpar)

Tests hanging on CI machines are often caused by async tests that are blocked waiting for results that will never complete. Unlike traditional synchronous hangs, attaching to an async hang and looking at executing threads typically will not provide anything useful as there won't be any threads actually executing the "awaiting"  methods.

This document walks through debugging a test hang using WinDBG and SOS.

## Prerequisites 

- [WinDBG](https://docs.microsoft.com/en-us/windows-hardware/drivers/debugger/debugger-download-tools)
- Basic understanding of async machinery  
- WinDBG attached to the hanging process, or opened with a dump from the hanging process

## Loading SOS

32-bit dump/process:

```
> .load C:\Windows\Microsoft.NET\Framework\v4.0.30319\sos.dll
```

64-bit dump/process:

```
> .load C:\Windows\Microsoft.NET\Framework64\v4.0.30319\sos.dll
```

## Determining the executing test

We need to find the xUnit types in memory that track test exception, in this case; [InvokeTestAsync](https://github.com/xunit/xunit/blob/9d10262a3694bb099ddd783d735aba2a7aac759d/src/xunit.execution/Sdk/Frameworks/Runners/XunitTestRunner.cs#L67). It is an async method, so we're going to find the blocked tests by looking through instances of its async state machine.

```
> !dumpheap -type InvokeTestAsync
```

```
 Address       MT     Size
036c8cb0 08389c3c       44     
037a1828 08389c3c       44     
039d9c40 08389c3c       44     
03adb7c4 08389c3c       44     
03b9cf68 08389c3c       44     
03bd7638 08389c3c       44     
03bf5748 08389c3c       44     
03d75740 08389c3c       44     
03d7fee0 08389c3c       44     
03dc019c 08389c3c       44     
03dc2e9c 08389c3c       44     
03dd51a4 08389c3c       44     
03ee8c00 08389c3c       44     
03f98a38 08389c3c       44     
03ff07e4 08389c3c       44     
0bff4e9c 08389c3c       44     
0c037b98 08389c3c       44     
0c05c2b0 08389c3c       44     

Statistics:
      MT    Count    TotalSize Class Name
08389c3c       18          792 Xunit.Sdk.XunitTestRunner+<InvokeTestAsync>d__4
Total 18 objects
```

The output tells me there's 18 instances of the async state machine in memory. What we are looking for are state machines that have not completed.

**Note:** *If you have Prefer DML (Command -> Prefer DML) turned on, the Address and MT (method table) column should be hyperlinked - you can just click the links instead of manually typing the commands.*

Let's dump the first one:

```
> !DumpObj /d 036c8cb0
```
```
Name:        Xunit.Sdk.XunitTestRunner+<InvokeTestAsync>d__4
MethodTable: 08389c3c
EEClass:     083b7c8c
Size:        44(0x2c) bytes
File:        E:\project-system2\artifacts\Debug\bin\UnitTests\xunit.execution.desktop.dll
Fields:
      MT    Field   Offset                 Type VT     Attr    Value Name
7243f2dc  4000237       14         System.Int32  1 instance       -2 <>1__state
015c84d8  4000238       18 ...lib]], mscorlib]]  1 instance 037a1840 <>t__builder
08383de4  4000239        4 ...k.XunitTestRunner  0 instance 036b0ca0 <>4__this
07869498  400023a        8 ...ceptionAggregator  0 instance 036b2d3c aggregator
7243d488  400023b        c        System.String  0 instance 03501228 <output>5__2
08389e5c  400023c       10 ....TestOutputHelper  0 instance 00000000 <testOutputHelper>5__3
015c8dc4  400023d       24 ...cimal, mscorlib]]  1 instance 037a184c <>u__1

```

The value of the `<>1__state` field is what we're interested in, this represents the "current state" of the state machine:

Value|Meaning
---:|---
-2|Finished executing
-1|Not started or currently executing (should be active call stack)
&gt;=0| Blocked in an await. The number indicates the zero-based ordinal of which await in the method is currently waiting. 

Above, the value in the statement is `-2` indicating that it has finished executing, and hence not the test we are looking for.

Let's dump the second one:

```
> !DumpObj /d 037a1828
```
```
Name:        Xunit.Sdk.XunitTestRunner+<InvokeTestAsync>d__4
MethodTable: 08389c3c
EEClass:     083b7c8c
Size:        44(0x2c) bytes
File:        E:\project-system2\artifacts\Debug\bin\UnitTests\xunit.execution.desktop.dll
Fields:
      MT    Field   Offset                 Type VT     Attr    Value Name
7243f2dc  4000237       14         System.Int32  1 instance        0 <>1__state
015c84d8  4000238       18 ...lib]], mscorlib]]  1 instance 036c8cc8 <>t__builder
08383de4  4000239        4 ...k.XunitTestRunner  0 instance 036b843c <>4__this
07869498  400023a        8 ...ceptionAggregator  0 instance 036b84e8 aggregator
7243d488  400023b        c        System.String  0 instance 03501228 <output>5__2
08389e5c  400023c       10 ....TestOutputHelper  0 instance 00000000 <testOutputHelper>5__3
015c8dc4  400023d       24 ...cimal, mscorlib]]  1 instance 036c8cd4 <>u__1
```

In the above state machine instance, the value of `<>1__state` is `0` indicating this currently blocked on the first await in [InvokeTestAsync](https://github.com/xunit/xunit/blob/9d10262a3694bb099ddd783d735aba2a7aac759d/src/xunit.execution/Sdk/Frameworks/Runners/XunitTestRunner.cs#L67) which executes the test. 

This indicates that we're interested in diving into this instance to find the hanging test.

To find the test class and test name, we need to dump the `<>4__this` field:

```
> !DumpObj /d 036b843c
```

```
Name:        Xunit.Sdk.XunitTestRunner
MethodTable: 08383de4
EEClass:     083b2410
Size:        48(0x30) bytes
File:        E:\project-system2\artifacts\Debug\bin\UnitTests\xunit.execution.desktop.dll
Fields:
      MT    Field   Offset                 Type VT     Attr    Value Name
07869498  400004e        4 ...ceptionAggregator  0 instance 036b8418 <Aggregator>k__BackingField
724834a4  400004f        8 ...lationTokenSource  0 instance 0367cb24 <CancellationTokenSource>k__BackingField
7243d87c  4000050        c      System.Object[]  0 instance 036a0148 <ConstructorArguments>k__BackingField
0707d53c  4000051       10 ...t.Sdk.IMessageBus  0 instance 0367d4e0 <MessageBus>k__BackingField
7243d488  4000052       14        System.String  0 instance 00000000 <SkipReason>k__BackingField
083850a4  4000053       18 ...bstractions.ITest  0 instance 036b83d8 <Test>k__BackingField
7243e688  4000054       1c          System.Type  0 instance 03554c1c <TestClass>k__BackingField
72442a84  4000055       20 ...ection.MethodInfo  0 instance 03585798 <TestMethod>k__BackingField
7243d87c  4000056       24      System.Object[]  0 instance 03569578 <TestMethodArguments>k__BackingField
08385180  400006d       28 ...ute, xunit.core]]  0 instance 036b8400 beforeAfterAttributes
```

To find the class, dump the ` <TestClass>k__BackingField` field:

```
> !DumpObj /d 03554c1c
```
```
Name:        System.RuntimeType
MethodTable: 7243e89c
EEClass:     72014fd0
Size:        28(0x1c) bytes
Type Name:   Microsoft.VisualStudio.ProjectSystem.OnceInitializedOnceDisposedUnderLockAsyncTests
Type MT:     07071d04
[...]
```

To find the method, dump the `<TestMethod>k__BackingField` field, followed by the `m_name` field:

```
> !DumpObj /d 03585798
```
```
Name:        System.Reflection.RuntimeMethodInfo
MethodTable: 7248e500
EEClass:     7202b1dc
Size:        60(0x3c) bytes
File:        C:\WINDOWS\Microsoft.Net\assembly\GAC_32\mscorlib\v4.0_4.0.0.0__b77a5c561934e089\mscorlib.dll
Fields:
      MT    Field   Offset                 Type VT     Attr    Value Name
72443db8  4001cb3       28        System.IntPtr  1 instance  7071cc0 m_handle
72443e20  4001cb4        4 ...+RuntimeTypeCache  0 instance 035853d4 m_reflectedTypeCache
7243d488  4001cb5        8        System.String  0 instance 03691920 m_name
[...]

```

```
> !DumpObj /d 03691920
```

```

Name:        System.String
MethodTable: 7243d488
EEClass:     72014a50
Size:        114(0x72) bytes
File:        C:\WINDOWS\Microsoft.Net\assembly\GAC_32\mscorlib\v4.0_4.0.0.0__b77a5c561934e089\mscorlib.dll
String:      ExecuteUnderLockAsync_AvoidsOverlappingWithDispose
[...]
```

Combining those, points us to the `OnceInitializedOnceDisposedUnderLockAsyncTests.ExecuteUnderLockAsync_AvoidsOverlappingWithDispose` method as the hanging test.

Based on what we learned above, we can dig further to figure out where it's hanging:

```
> !dumpheap -type ExecuteUnderLockAsync_AvoidsOverlappingWithDispose
```

```
 Address       MT     Size
036c8440 088fdfb4       60     

Statistics:
      MT    Count    TotalSize Class Name
088fdfb4        1           60 Microsoft.VisualStudio.ProjectSystem.OnceInitializedOnceDisposedUnderLockAsyncTests+<ExecuteUnderLockAsync_AvoidsOverlappingWithDispose>d__9
Total 1 objects

```

Dump that first address:

```
> !DumpObj /d 036c8440
```
```
Name:        Microsoft.VisualStudio.ProjectSystem.OnceInitializedOnceDisposedUnderLockAsyncTests+<ExecuteUnderLockAsync_AvoidsOverlappingWithDispose>d__9
MethodTable: 088fdfb4
EEClass:     083e5d80
Size:        60(0x3c) bytes
File:        E:\project-system2\artifacts\Debug\bin\UnitTests\Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests.dll
Fields:
      MT    Field   Offset                 Type VT     Attr    Value Name
7243f2dc  4000266       20         System.Int32  1 instance        2 <>1__state
72489528  4000267       24 ...TaskMethodBuilder  1 instance 036c8464 <>t__builder
72485c7c  4000268        4 ...eading.Tasks.Task  0 instance 036c7788 firstAction
72485c7c  4000269        8 ...eading.Tasks.Task  0 instance 036c791c secondAction
083ce314  400026a        c ...cManualResetEvent  0 instance 036b8740 firstEntered
083ce314  400026b       10 ...cManualResetEvent  0 instance 036b879c firstRelease
083ce314  400026c       14 ...cManualResetEvent  0 instance 036b87f8 secondEntered
07071d04  400026d       18 ...derLockAsyncTests  0 instance 036b8600 <>4__this
088fe084  400026e       1c ...__DisplayClass9_0  0 instance 036c847c <>8__1
7247ddf4  400026f       30 ...vices.TaskAwaiter  1 instance 036c8470 <>u__1
015c5230  4000270       34 ...ption, mscorlib]]  1 instance 036c8474 <>u__2
```
`<>1__state` has a value of `2`, which as per above table indicates that 3rd await within the method currently blocked:

``` C#
[Fact]
public void ExecuteUnderLockAsync_AvoidsOverlappingWithDispose()
{

    [...]

    await firstEntered.WaitAsync();
    await Assert.ThrowsAsync<TimeoutException>(() => secondEntered.WaitAsync().WithTimeout(TimeSpan.FromMilliseconds(50)));

    firstRelease.Set();
    await secondEntered.WaitAsync(); // <!-- blocked here
    await Task.WhenAll(firstAction, secondAction);

    [...]
}
```

That's the line that you should start investigating.

## Further information

You can see more information, including a WinDBG extension that dumps async callstacks, over on [Async Hang Investigations](https://github.com/Microsoft/vs-threading/blob/master/doc/async_hang.md).