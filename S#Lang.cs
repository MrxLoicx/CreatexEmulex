using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using System.Text;
namespace S_
{
	public static class CSharpCode
	{
		public static void Compile(string _source) {
			string source = _source;
			// Настройки компиляции
            Dictionary<string, string> providerOptions = new Dictionary<string, string>();
            providerOptions.Add("CompilerVersion", "v4.0");
            CSharpCodeProvider provider = new CSharpCodeProvider(providerOptions);
            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.OutputAssembly = @"C:\Users\fdshfgas\Desktop\Code.exe";
            compilerParams.GenerateExecutable = true;
            
            if (Directives.IsUnsafeCode(source))
            	compilerParams.CompilerOptions += "-unsafe ";
            if (Directives.IsOptimizeCode(source))
            	compilerParams.CompilerOptions += "-optimize ";
			if (Directives.IsDebugCode(source))          
				compilerParams.CompilerOptions += "-debug ";				
            
            source = source.Replace("#unsafe enable", "")
            	.Replace("#optimize +", "")
            	.Replace("#debug", "");
            
            compilerParams.ReferencedAssemblies.Add("System.Drawing.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            compilerParams.ReferencedAssemblies.Add("System.Linq.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.Linq.dll");
            compilerParams.ReferencedAssemblies.Add("System.Data.dll");
            // Компиляция
            CompilerResults results = provider.CompileAssemblyFromSource(compilerParams, source);

            if (results.Errors.Count == 0) {
            	Process.Start(compilerParams.OutputAssembly);
            } else {
            	StringBuilder sb = new StringBuilder();
            	
            	foreach (CompilerError err in results.Errors)
            	{
                	sb.AppendLine("Line " + err.Line + ": " + err.ErrorText);
            	}
            	
            	Console.WriteLine(sb.ToString());
            }
		}
	}
	
	/// Описывает директивы препроцессора 
	public static class Directives
	{
		// Содержит ли код данную директиву
		private static bool ContainsDirective(string source, string directive)
		{
			bool isexist = false;
			if (source.Contains(directive)) {
				isexist = true;
			}
			return isexist;
		}
		
		/// Есть ли в коде директива #unsafe enable
		public static bool IsUnsafeCode(string source)
		{
			return ContainsDirective(source, "#unsafe enable");
		}
		
		/// Есть ли в коде директива #optimize +
		public static bool IsOptimizeCode(string source)
		{
			return ContainsDirective(source, "#optimize +");
		}
		
		/// Есть ли в коде директива #debug
		public static bool IsDebugCode(string source)
		{
			return ContainsDirective(source, "#debug");
		}
	}
	// Набор инструкций характерных только для языка S#
	public class Instructions
	{
		public Dictionary<string, string> instructions = new Dictionary<string, string>();
		
		public Instructions()
		{
			instructions.Add("crate ", "using ");
			instructions.Add("let ", "var ");
			instructions.Add("__mainx__()", "public static void Main(string[] args)");
			instructions.Add("elif ", "else if ");
			instructions.Add("?", "== null");
			instructions.Add("str ", "string ");
			instructions.Add("(str)", "(string)");
			instructions.Add("i8 ", "byte ");
			instructions.Add("i16 ", "short ");
			instructions.Add("i32 ", "int ");
			instructions.Add("i64 ", "long ");
			instructions.Add("f32 ", "float ");
			instructions.Add("(i8)", "(byte)");
			instructions.Add("(i16)", "(short)");
			instructions.Add("(i32)", "(int)");
			instructions.Add("(i64)", "(long)");
			instructions.Add("(f32)", "(float)");
			instructions.Add("overt ", "public static ");
			instructions.Add("ret ", "return ");
			instructions.Add("inc ", "++ ");
			instructions.Add("dec ", "-- ");
		}
	}
	
	public static class SLang
	{
		public static void Compile(string source)
		{
			CSharpCode.Compile(Modify(source));
		}
		/// Показать код C# который получится из S#
		public static string ShowCodeCSharp(string source_scode)
		{
			return Modify(source_scode);
		}
		/// Изменяет код S# для компиляции как C#
		public static string Modify(string source) 
		{
			return CycleLoop.Replace(AdapterSCodeToCCode.ReplaceInstructions(source));
		}
	}
	
	internal static class AdapterSCodeToCCode
	{
		/// Заменяет инструкции на языке S# на C#
		public static string ReplaceInstructions(string source_s)
		{
			Instructions Instr = new Instructions();
			string key, val;
			foreach (KeyValuePair<string, string> valuePair in Instr.instructions) {
				key = valuePair.Key;
				val = valuePair.Value;
				source_s = source_s.Replace(key, val);
			}
			return source_s;
		}
	}
	/// Реализует цикл loop(x)
	internal static class CycleLoop
	{
		/// Заменяет инструкцию вида loop (x) на for(int i = 0; i < x; ++i)
		public static string Replace(string source)
		{
			Regex regex = new Regex("loop", RegexOptions.Multiline);
			Match match = regex.Match(source);
			
			while(match.Success) {
				string countIterations = source.Substring(match.Index + 5, 1).Trim();
				string value = match.Value + "(" + countIterations + ") {";
				source = LoopToFor(source, value, countIterations);
				match = regex.Match(source);
			}
			
			return source;
		}
		
		private static string LoopToFor(string source,string loopStr, string countIterations) 
		{
			string forStr = "for(int i = 0; i < " + countIterations + "; ++i) {";
			return source.Replace(loopStr, forStr);
		}
	}
	
	class Program
	{
		public static void Main(string[] args)
		{
			#region Test Examples
			string scode_1 = @"
			#unsafe enable
			crate System;
			crate System.Runtime.InteropServices;
			
			namespace Ptr_
{
    static class Program
    {
        static void Main(string[] args)
        {
        	unsafe {
        		const int size = 8;
        		int* factorial = stackalloc int[size]; // выделяем память в стеке под 8 объектов int
        		int* p = factorial;
        		
        		// присваиваем первой ячейке значение 1 
        		*(p) = 1;
        		// считаем факториал со 2-го числа и дальше 
        		p++;
        		for (int i = 2; i <= size; i++) {
        			//считаем факториал числа
        			*p = p[-1] * i;
        			// следующая ячейка
        			p++;
        		}
        		
        		// Вывод всех факториалов
        		for (int i = 1; i <= size; i++) {
        			Console.WriteLine(factorial[i-1]);
        		}
        	}
        	
            Console.ReadKey();
        }
    }
}
";
			
			string scode_2 = @"
			crate System;

namespace N
{
	overt class Program
	{
		__mainx__()
		{
			i32 j = 0;
			loop(5) {
				Console.WriteLine(++j);
			}
			
			loop(2) {
				for(i32 k = 0;  k < 2; ++k) {
					Console.WriteLine(k);
				}
				Console.WriteLine(555);
			}
			Console.ReadLine();
		}
	}
}";
			
			string scode = @"
			#optimize +
			crate System;

namespace N
{
	overt class Program
	{
		__mainx__()
		{
			i32 b = 8;
			i32 h = inc b;
			Console.WriteLine(h);
			Console.ReadLine();
		}
	}
}";
			
			#endregion
			
			SLang.Compile(scode);
			Console.WriteLine(SLang.ShowCodeCSharp(scode));
			
			Console.ReadKey(true);
		}
	}
}
