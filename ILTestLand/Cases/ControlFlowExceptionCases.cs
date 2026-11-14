using System.Collections;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 7, OpaqueDepth = 4,
    DecryptHashChance = 0.7)]
internal static class ControlFlowExceptionCases {
    public static bool Case0801() { try { throw new InvalidOperationException("801"); } catch (InvalidOperationException error) { return error.Message == "801"; } }
    public static bool Case0802() { try { _ = 1 / int.Parse("0"); return false; } catch (DivideByZeroException) { return true; } }
    public static bool Case0803() { var state = 0; try { state = 3; } finally { state += 800; } return state == 803; }
    public static bool Case0804() { var state = ""; try { try { throw new ArgumentException("804"); } finally { state += "f"; } } catch (ArgumentException error) { state += error.Message; } return state == "f804"; }
    public static bool Case0805() { try { throw new CaseException(805); } catch (CaseException error) when (error.Code == 805) { return true; } }
    public static bool Case0806() { try { throw new CaseException(806); } catch (CaseException error) when (error.Code == 0) { return false; } catch (CaseException error) { return error.Code == 806; } }
    public static bool Case0807() { var filters = 0; try { throw new CaseException(807); } catch (CaseException error) when (CountFilter(error, ref filters, false)) { return false; } catch (CaseException error) when (CountFilter(error, ref filters, true)) { return filters == 2; } }
    public static bool Case0808() { try { ThrowNested(808); return false; } catch (CaseException error) { return error.Code == 808 && error.InnerException is InvalidOperationException; } }
    public static bool Case0809() { Exception error = new AggregateException(new InvalidOperationException("809")); return ((AggregateException)error).Flatten().InnerExceptions.Single().Message == "809"; }
    public static bool Case0810() { try { checked { _ = int.MaxValue + int.Parse("1"); } return false; } catch (OverflowException) { return true; } }
    public static bool Case0811() { try { _ = int.Parse("not-811"); return false; } catch (FormatException error) { return error is not null; } }
    public static bool Case0812() { try { var values = new int[1]; _ = values[12]; return false; } catch (IndexOutOfRangeException) { return true; } }
    public static bool Case0813() { try { ArgumentNullException.ThrowIfNull((object?)null, "case813"); return false; } catch (ArgumentNullException error) { return error.ParamName == "case813"; } }
    public static bool Case0814() { try { ArgumentOutOfRangeException.ThrowIfNegative(-814, "value"); return false; } catch (ArgumentOutOfRangeException error) { return error.ParamName == "value"; } }
    public static bool Case0815() { var stream = new MemoryStream(); stream.Dispose(); try { _ = stream.ReadByte(); return false; } catch (ObjectDisposedException) { return true; } }
    public static bool Case0816() { var disposed = 0; using (new Disposable(() => disposed += 800)) using (new Disposable(() => disposed += 16)) { } return disposed == 816; }
    public static bool Case0817() { var order = ""; using (new Disposable(() => order += "a")) using (new Disposable(() => order += "b")) order += "x"; return order == "xba"; }
    public static bool Case0818() { var disposed = false; try { using var value = new Disposable(() => disposed = true); throw new CaseException(818); } catch (CaseException error) { return error.Code == 818 && disposed; } }
    public static bool Case0819() { var gate = new object(); var entered = false; lock (gate) { entered = Monitor.IsEntered(gate); } return entered && !Monitor.IsEntered(gate); }
    public static bool Case0820() { var gate = new object(); Monitor.Enter(gate); try { return Monitor.IsEntered(gate); } finally { Monitor.Exit(gate); } }
    public static bool Case0821() { var sum = 0; for (var outer = 0; outer < 5; outer++) for (var inner = 0; inner < 5; inner++) { if (inner == 3) break; sum += outer + inner; } return sum == 45; }
    public static bool Case0822() { var sum = 0; for (var value = 0; value < 20; value++) { if (value % 3 != 0) continue; sum += value; } return sum == 63; }
    public static bool Case0823() { var value = 0; Start: value++; if (value < 8) goto Start; return value == 8; }
    public static bool Case0824() { var value = 824; return value switch { < 0 => -1, >= 800 and < 900 when value % 2 == 0 => 824, _ => 0 } == 824; }
    public static bool Case0825() { object value = new Model(825, "case"); return value is Model { Id: 825, Name.Length: 4 }; }
    public static bool Case0826() { int[] values = [8, 2, 6]; return values is [8, .. var middle, 6] && middle.Single() == 2; }
    public static bool Case0827() { var value = (Code: 827, Text: "ok"); var (code, text) = value; return code == 827 && text == "ok"; }
    public static bool Case0828() { var first = 800; var second = 28; (first, second) = (second, first); return first == 28 && second == 800; }
    public static bool Case0829() { var calls = 0; int Read() { calls++; return 829; } var result = true ? Read() : Read() + 1; return result == 829 && calls == 1; }
    public static bool Case0830() { string? value = null; var created = value ??= "830"; return ReferenceEquals(value, created) && value == "830"; }
    public static bool Case0831() { Model? model = null; return (model?.Name.Length ?? 831) == 831; }
    public static bool Case0832() { var result = Enumerable.Range(1, 10).Aggregate(0, (total, value) => value % 2 == 0 ? total + value : total); return result == 30; }
    public static bool Case0833() { var state = 0; var result = 0; try { for (var i = 0; i < 10; i++) { if (i == 3) { result = state + 830; break; } state++; } } finally { state += 1000; } return result == 833 && state == 1003; }
    public static bool Case0834() { var values = YieldUntil(10, 4).ToArray(); return values.SequenceEqual(new[] { 0, 1, 2, 3 }) && values.Sum() == 6; }
    public static bool Case0835() { var finallyRan = false; using var enumerator = YieldWithFinally(() => finallyRan = true).GetEnumerator(); _ = enumerator.MoveNext(); enumerator.Dispose(); return finallyRan; }
    public static bool Case0836() { try { Task.FromException(new CaseException(836)).GetAwaiter().GetResult(); return false; } catch (CaseException error) { return error.Code == 836; } }
    public static bool Case0837() { try { Task.WhenAll(Task.FromException(new CaseException(837)), Task.CompletedTask).GetAwaiter().GetResult(); return false; } catch (CaseException error) { return error.Code == 837; } }
    public static bool Case0838() { var task = Task.Run(() => { throw new CaseException(838); }); try { task.Wait(); return false; } catch (AggregateException error) { return error.InnerException is CaseException { Code: 838 }; } }
    public static bool Case0839() { using var source = new CancellationTokenSource(); source.Cancel(); var task = Task.FromCanceled(source.Token); return task.IsCanceled && task.Exception is null; }
    public static bool Case0840() { var completion = new TaskCompletionSource<int>(); return completion.TrySetException(new CaseException(840)) && completion.Task.IsFaulted && completion.Task.Exception!.InnerException is CaseException { Code: 840 }; }
    public static bool Case0841() { var count = 0; try { Retry(); } catch (CaseException error) { return count == 3 && error.Code == 841; } return false; void Retry() { while (true) { count++; try { if (count < 3) throw new InvalidOperationException(); throw new CaseException(841); } catch (InvalidOperationException) { continue; } } } }
    public static bool Case0842() { var path = new[] { 8, 4, 2 }; var result = 0; foreach (var value in path) { try { result = checked(result * 10 + value); } catch (OverflowException) { return false; } } return result == 842; }
    public static bool Case0843() { var value = 843; try { return value == 843; } finally { value = 0; } }
    public static bool Case0844() { var result = 0; try { throw new CaseException(844); } catch (CaseException error) { result = error.Code; } finally { result += 1; } return result == 845; }
    public static bool Case0845() { Exception error = new CaseException(845); return error switch { CaseException { Code: >= 800 } found => found.Code == 845, _ => false }; }
    public static bool Case0846() { var queue = new Queue<Func<int>>(); queue.Enqueue(() => 800); queue.Enqueue(() => 40); queue.Enqueue(() => 6); var sum = 0; while (queue.TryDequeue(out var work)) sum += work(); return sum == 846; }
    public static bool Case0847() { var actions = new List<Action>(); var values = new List<int>(); for (var i = 0; i < 3; i++) { var copy = i; actions.Add(() => values.Add(copy)); } foreach (var action in actions) action(); return values.SequenceEqual(new[] { 0, 1, 2 }); }
    public static bool Case0848() { var result = Apply(800, value => value + 48); return result == 848; static int Apply(int value, Func<int, int> transform) => transform(value); }
    public static bool Case0849() { var first = new DisposableCounter(); var second = new DisposableCounter(); try { using (first) using (second) throw new CaseException(849); } catch (CaseException) { return first.Count == 1 && second.Count == 1; } }
    public static bool Case0850() { var trace = new List<int>(); try { trace.Add(8); try { trace.Add(5); throw new CaseException(850); } catch (CaseException) { trace.Add(0); throw; } finally { trace.Add(1); } } catch (CaseException error) { return error.Code == 850 && trace.SequenceEqual(new[] { 8, 5, 0, 1 }); } }

    private static bool CountFilter(CaseException error, ref int count, bool result) {
        count++;
        return error.Code > 0 && result;
    }

    private static void ThrowNested(int code) {
        try {
            throw new InvalidOperationException("inner");
        }
        catch (InvalidOperationException error) {
            throw new CaseException(code, error);
        }
    }

    private static IEnumerable<int> YieldUntil(int count, int stop) {
        for (var index = 0; index < count; index++) {
            if (index == stop)
                yield break;
            yield return index;
        }
    }

    private static IEnumerable<int> YieldWithFinally(Action cleanup) {
        try {
            yield return 1;
            yield return 2;
        }
        finally {
            cleanup();
        }
    }

    private sealed record Model(int Id, string Name);

    private sealed class CaseException : Exception {
        public int Code { get; }

        public CaseException(int code) : base(code.ToString()) {
            Code = code;
        }

        public CaseException(int code, Exception innerException) : base(code.ToString(), innerException) {
            Code = code;
        }
    }

    private sealed class Disposable(Action dispose) : IDisposable {
        public void Dispose() => dispose();
    }

    private sealed class DisposableCounter : IDisposable {
        public int Count { get; private set; }
        public void Dispose() => Count++;
    }
}
