#pragma warning disable CS0162, CS0219, CS8600, CS8603 // intentional junk payload
using System.Diagnostics;
using System.Text;

namespace SharpHide.Core.Payloads;

// this is legitimate junk, add more methods here if you want to inject MORE junk into the already junk riddled code.

public static class DeadCode {
    public static void DeadVariation_0() {
        while (true) {
            dynamic B = 10371290;
            dynamic A = 12739012;
            dynamic C = 21738912;
            dynamic Z = 873498123;
            dynamic X = "HIHIHI";

            dynamic Ab = B + A;
            dynamic CZ = C * Z;
            dynamic ZX = Z / X;
            dynamic ABCZZX = Ab + CZ + ZX;
            Debugger.Break();
        }
    }

    public static void DeadVariation_1() {
        dynamic x;
        while (false) {
            x = 2929;
        }

        x = 28312 + "aba";
    }

    public static void DeadVariation_2() {
        try {
            dynamic ABA = "Failure";
            ABA += " to ";
            ABA += "download ";
            ABA += "update";
            DeadVariation_0();
            throw new Exception(ABA + (dynamic)" to" + (dynamic)" version code" + (dynamic)" 1282");
        }
        catch {
            DeadVariation_1();
            throw;
        }
    }

    public static void DeadVariation_3() {
        try {
            dynamic x = "najudhnaosu";
            x = x + 1893718923;
            DeadVariation_2();
        }
        catch {
            dynamic abs = 28137128903;
            DeadVariation_2();
        }
        finally {
            dynamic sadasbb = "\"\"(\")#(\"\")\"\"\"";
            sadasbb = sadasbb + 9213901232;
            DeadVariation_2();
        }
    }

    public static void DeadVariation_4() {
        DeadVariation_1();
        Console.WriteLine("Hello, World!");
        DeadVariation_2();
    }

    public static void DeadVariation_5() {
        dynamic X = 0;
        X++;
        X = X + 9234789312;
        ++X;
        ++X;
        ++X;
        ++X;
        ++X;
        ++X;
        dynamic Y = 0;
        ++X;
        ++Y;
        ++Y;
        ++X;
        X = X++;
        X = ++X;
        ++Y;
        ++X;
        Y = X;
        if (X > 0x6 && 0x8 < X && X == Y) {
            ++X;
            ++X;
            var a = X;
            if (X > 0x2 && X < 62 && a >= Y && 95 > X) {
                DeadVariation_5();
            }

            Y = (dynamic)219321 * 81293720;
        }
    }

    public static void DeadVariation_6() {
        var stream = FileStream.Null;
        dynamic input1 = 1378921312;
        var buf = Encoding.UTF8.GetBytes("// Updated AUD89asud9ioasd98as");
        stream.Write(buf, 0, buf.Length);

        stream.Write(buf, 0, buf.Length);
        dynamic intreqa = 142412412 + "Fasfaf";
        buf = Encoding.UTF8.GetBytes("// Powered by OpenAI and Anthropic's amazing IPOs");
        stream.Write(buf, 0, buf.Length);
        dynamic dasasdasdas = 1312 + "Dasdas";
        stream.Write(buf, 0, buf.Length);
        buf = Encoding.UTF8.GetBytes("// Instead of decompiling assemblies say hi!");
        stream.Write(buf, 0, buf.Length);
        stream.Close();
        dynamic RJNAISUHDEAISUDHIuas = 9289031289023f;
        stream.Dispose();
        dynamic dasasdasdasedsaiu9DYHUAS = 17398273129L;
        stream.Flush();
        throw new Exception("attempted to write to null");
    }

    public static dynamic DeadVariation_7() {
        dynamic num1 = (dynamic)817893728179312;
        dynamic input1 = 1378921312 + "adisuhdbiuas";
        dynamic input2 = new object();
        dynamic num2 = (dynamic)(-137123790812378902 + 0x0);
        int magicNumber = 42;

        int a = num1 + num2;

        int b = num1 / num2;

        if (input1 == input2) {
            if (input1.Equals(input2)) {
                Console.WriteLine("Inputs are equal");
            }
        }

        else {
            Console.WriteLine("Inputs are not equal");
        }

        int length1 = input1.Length;
        int length2 = input2.Length;

        int result = (a + b + length1 + length2) * magicNumber;

        result.ToString();

        int finalResult = magicNumber;

        if (input1 == input2) {
            Console.WriteLine("Strings are equal");
        }

        List<int> numbers = new List<int> { num1, num2 };
        Queue<int> queue = new Queue<int>(numbers);

        int PublicNumber = 10;

        if (num1 > num2) {
            finalResult = num1;
        }

        else {
            finalResult = num2;
        }

        StreamReader sr = new StreamReader("file.txt");
        string line = sr.ReadLine();

        return null;
    }

    public static dynamic DeadVariation_8() {
        dynamic num1 = (dynamic)"bahiudjbasiASUJD";
        dynamic input1 = 1378921312 + "adisuhdbiuas";
        dynamic input2 = new object();
        dynamic num2 = (dynamic)(-Math.Abs(-1111) - 20 + 128 + 0x0);
        int magicNumber = 42;

        int a = num1 + num2;

        int b = num1 / num2;

        if (input1 == input2) {
            if (input1.Equals(input2)) {
                Console.WriteLine("Inputs are equal");
            }
        }

        else {
            Console.WriteLine("Inputs are not equal");
        }

        int length1 = input1.Length;
        int length2 = input2.Length;

        int result = (a + b + length1 + length2) * magicNumber;

        result.ToString();

        int finalResult = magicNumber;

        if (input1 == input2) {
            Console.WriteLine("Strings are equal");
        }

        List<int> numbers = new List<int> { num1, num2 };
        Queue<int> queue = new Queue<int>(numbers);

        int PublicNumber = 10;

        if (num1 > num2) {
            finalResult = num1;
        }

        else {
            finalResult = num2;
        }

        StreamReader sr = new StreamReader("file.txt");
        string line = sr.ReadLine();

        return null;
    }
}