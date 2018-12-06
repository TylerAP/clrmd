using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Runtime.Tests {
    public class TestOutputHelper : TextWriter, ITestOutputHelper
    {
        private readonly ITestOutputHelper _output;

        public TestOutputHelper(ITestOutputHelper helper) =>
            _output = helper;

        public override void Close()
        {
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync()
        {
            return Task.FromResult<object>(null);
        }

        public override void Write(bool value)
        {
            throw new NotImplementedException();
        }

        public override void Write(char value)
        {
            throw new NotImplementedException();
        }

        public override void Write(char[] buffer)
        {
            throw new NotImplementedException();
        }

        public override void Write(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(decimal value)
        {
            throw new NotImplementedException();
        }

        public override void Write(double value)
        {
            throw new NotImplementedException();
        }

        public override void Write(int value)
        {
            throw new NotImplementedException();
        }

        public override void Write(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(object value)
        {
            throw new NotImplementedException();
        }

        public override void Write(float value)
        {
            throw new NotImplementedException();
        }

        public override void Write(string value)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, object arg0)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, object arg0, object arg1)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, object arg0, object arg1, object arg2)
        {
            throw new NotImplementedException();
        }

        public override void Write(string format, params object[] arg)
        {
            throw new NotImplementedException();
        }

        public override void Write(uint value)
        {
            throw new NotImplementedException();
        }

        public override void Write(ulong value)
        {
            throw new NotImplementedException();
        }

        public override void Write(ReadOnlySpan<char> buffer)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(char value)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task WriteAsync(string value)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine()
        {
            _output.WriteLine("");
        }

        public override void WriteLine(bool value)
        {
            _output.WriteLine(value.ToString());
        }

        public override void WriteLine(char value)
        {
            _output.WriteLine(value.ToString());
        }

        public override void WriteLine(char[] buffer)
        {
            _output.WriteLine(new string(buffer));
        }

        public override void WriteLine(char[] buffer, int index, int count)
        {
            _output.WriteLine(new string(buffer, index, count));
        }

        public override void WriteLine(decimal value)
        {
            _output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public override void WriteLine(double value)
        {
            _output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public override void WriteLine(int value)
        {
            _output.WriteLine(value.ToString());
        }

        public override void WriteLine(long value)
        {
            _output.WriteLine(value.ToString());
        }

        public override void WriteLine(object value)
        {
            _output.WriteLine(value.ToString());
        }

        public override void WriteLine(float value)
        {
            _output.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        }

        public override void WriteLine(string value)
        {
            _output.WriteLine(value);
        }

        void ITestOutputHelper.WriteLine(string format, params object[] args)
        {
            _output.WriteLine(format, args);
        }

        public override void WriteLine(string format, object arg0)
        {
            _output.WriteLine(format, arg0);
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            _output.WriteLine(format, arg0, arg1);
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            _output.WriteLine(format, arg0, arg1, arg2);
        }

        void ITestOutputHelper.WriteLine(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string format, params object[] arg)
        {
            _output.WriteLine(format, arg);
        }

        public override void WriteLine(uint value)
        {
            _output.WriteLine(value.ToString());
        }

        public override void WriteLine(ulong value)
        {
            _output.WriteLine(value.ToString());
        }

        public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            _output.WriteLine(new string(buffer));
        }

        public override async Task WriteLineAsync()
        {
            await Task.Run(() => WriteLine());
        }

        public override async Task WriteLineAsync(char value)
        {
            await Task.Run(() => WriteLine(value));
        }

        public override async Task WriteLineAsync(char[] buffer, int index, int count)
        {
            await Task.Run(() => WriteLine(buffer, index, count));
        }

        public override async Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            await Task.Run(() => WriteLine(buffer), cancellationToken);
        }

        public override async Task WriteLineAsync(string value)
        {
            await Task.Run(() => WriteLine(value));
        }

        public override Encoding Encoding => Encoding.UTF8;
        public override IFormatProvider FormatProvider => CultureInfo.InvariantCulture;
        public override string NewLine { get; set; } = "\n";
    }
}