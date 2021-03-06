(*** hide ***)
#I @"../../src/FsCheck/bin/Release"
#r @"FsCheck"
open FsCheck
open System

(**
# Model-based Testing

FsCheck also allows you to test objects, which usually encapsulate internal 
state through a set of methods. FsCheck, through small extension, 
allows you to do model-based specification of a class under test. Consider the following class, 
with an artificial bug in it:*)

type Counter() =
  let mutable n = 0
  member __.Inc() = n <- n + 1
  member __.Dec() = if n > 2 then n <- n - 2 else n <- n - 1
  member __.Get = n
  member __.Reset() = n <- 0
  override __.ToString() = n.ToString()

(**
We'll elide the class definition in C#, it's very similar.

As a model to test this class we can use an int value which is an abstraction of the object's internal state. The
idea is that each operation on the class (in this case, Inc and Dec) affects both the model and the actual object, and 
after each such operation, the model and the actual instance should still be equivalent.

With this idea in mind, you can write a specification of the Counter class using an int model as follows (full example
in C# below):*)

let spec =
  let inc = //specify what happens to model and actual instance when `Inc()` is called.
      { new Command<Counter,int>() with
          member x.RunActual c = c.Inc(); c
          member x.RunModel m = m + 1
          member x.Post (c,m) = m = c.Get |> Prop.ofTestable //the property that must hold after each invocation.
          override x.ToString() = "inc"}
  let dec = //specify what happens to model and actual instance when `Dec()` is called.
      { new Command<Counter,int>() with
          member x.RunActual c = c.Dec(); c
          member x.RunModel m = m - 1
          member x.Post (c,m) = m = c.Get |> Prop.ofTestable
          override x.ToString() = "dec"}
  { new ICommandGenerator<Counter,int> with
      member __.InitialActual = Counter()
      member __.InitialModel = 0
      member __.Next model = Gen.elements [inc;dec] }

(**
A specification is put together for FsCheck as an object that implementents `ICommandGenerator<'typeUnderTest,'modelType>`. It should return 
an initial object and an initial model of that object; and it should return a generator of `Command` objects.

Each `Command` typically represents one method to call on the object under test, and describes what 
happens to the model and the object when the command is executed. Also, it can assert preconditions that 
need to hold before executing the command: FsCheck will not execute that command if the precondition does 
not hold. It asserts postconditions that should hold after a command is executed: FsCheck 
fails the test if a postcondition does not hold.

Preferably also override ToString in each command so that counterexamples can be printed.

A specification can be checked as follows:*)

(***define-output:spec***)
Check.Quick (Command.toProperty spec)

(***include-output:spec***)

(**
Notice that not only has FsCheck found our 'bug', it has also produced the minimal sequence that leads to it.

Finally, in C#, all this looks as follows:

    [lang=csharp,file=../csharp/StatefulTesting.cs,key=counterspec]

And to run:

    [lang=csharp,file=../csharp/StatefulTesting.cs,key=check]

*)
