﻿namespace BundleTransformer.Less.Compilers
{
	using System;
	using System.Linq;
	using System.Text;

	using MsieJavaScriptEngine;
	using MsieJavaScriptEngine.ActiveScript;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	using Core;
	using Core.Assets;
	using Core.SourceCodeHelpers;
	using CoreStrings = Core.Resources.Strings;

	/// <summary>
	/// LESS-compiler
	/// </summary>
	internal sealed class LessCompiler : IDisposable
	{
		/// <summary>
		/// Name of resource, which contains a LESS-library
		/// </summary>
		private const string LESS_LIBRARY_RESOURCE_NAME = "BundleTransformer.Less.Resources.less-combined.min.js";

		/// <summary>
		/// Name of resource, which contains a LESS-compiler helper
		/// </summary>
		private const string LESSC_HELPER_RESOURCE_NAME = "BundleTransformer.Less.Resources.lesscHelper.min.js";

		/// <summary>
		/// Template of function call, which is responsible for compilation
		/// </summary>
		private const string COMPILATION_FUNCTION_CALL_TEMPLATE = @"lessHelper.compile({0}, {1}, {2}, {3});";

		/// <summary>
		/// String representation of the default compilation options
		/// </summary>
		private readonly string _defaultOptionsString;

		/// <summary>
		/// MSIE JS engine
		/// </summary>
		private MsieJsEngine _jsEngine;

		/// <summary>
		/// Synchronizer of compilation
		/// </summary>
		private readonly object _compilationSynchronizer = new object();

		/// <summary>
		/// Flag that compiler is initialized
		/// </summary>
		private bool _initialized;

		/// <summary>
		/// Flag that object is destroyed
		/// </summary>
		private bool _disposed;


		/// <summary>
		/// Constructs instance of LESS-compiler
		/// </summary>
		public LessCompiler() : this(null)
		{ }

		/// <summary>
		/// Constructs instance of LESS-compiler
		/// </summary>
		/// <param name="defaultOptions">Default compilation options</param>
		public LessCompiler(CompilationOptions defaultOptions)
		{
			_defaultOptionsString = (defaultOptions != null) ?
				ConvertCompilationOptionsToJson(defaultOptions).ToString() : "null";
		}

		/// <summary>
		/// Destructs instance of LESS-compiler
		/// </summary>
		~LessCompiler()
		{
			Dispose(false /* disposing */);
		}


		/// <summary>
		/// Initializes compiler
		/// </summary>
		private void Initialize()
		{
			if (!_initialized)
			{
				Type type = GetType();

				_jsEngine = new MsieJsEngine(true, true);
				_jsEngine.ExecuteResource(LESS_LIBRARY_RESOURCE_NAME, type);
				_jsEngine.ExecuteResource(LESSC_HELPER_RESOURCE_NAME, type);

				_initialized = true;
			}
		}

		/// <summary>
		/// "Compiles" LESS-code to CSS-code
		/// </summary>
		/// <param name="content">Text content written on LESS</param>
		/// <param name="path">Path to LESS-file</param>
		/// <param name="dependencies">List of dependencies</param>
		/// <param name="options">Compilation options</param>
		/// <returns>Translated LESS-code</returns>
		public string Compile(string content, string path, DependencyCollection dependencies,
			CompilationOptions options = null)
		{
			string newContent;
			string currentOptionsString = (options != null) ? 
				ConvertCompilationOptionsToJson(options).ToString() : _defaultOptionsString;

			lock (_compilationSynchronizer)
			{
				Initialize();

				try
				{
					var result = _jsEngine.Evaluate<string>(string.Format(COMPILATION_FUNCTION_CALL_TEMPLATE,
						JsonConvert.SerializeObject(content),
						JsonConvert.SerializeObject(path),
						ConvertDependenciesToJson(dependencies),
						currentOptionsString));
					var json = JObject.Parse(result);

					var errors = json["errors"] != null ? json["errors"] as JArray : null;
					if (errors != null && errors.Count > 0)
					{
						throw new LessCompilingException(FormatErrorDetails(errors[0], content, path, 
							dependencies));
					}

					newContent = json.Value<string>("compiledCode");
				}
				catch (ActiveScriptException e)
				{
					throw new LessCompilingException(
						ActiveScriptErrorFormatter.Format(e));
				}
			}

			return newContent;
		}

		/// <summary>
		/// Converts a list of dependencies to JSON
		/// </summary>
		/// <param name="dependencies">List of dependencies</param>
		/// <returns>List of dependencies in JSON format</returns>
		private static JArray ConvertDependenciesToJson(DependencyCollection dependencies)
		{
			var dependenciesJson = new JArray(
				dependencies.Select(d => new JObject(
					new JProperty("path", d.Url),
					new JProperty("content", d.Content))
				)
			);

			return dependenciesJson;
		}

		/// <summary>
		/// Converts a compilation options to JSON
		/// </summary>
		/// <param name="options">Compilation options</param>
		/// <returns>Compilation options in JSON format</returns>
		private static JObject ConvertCompilationOptionsToJson(CompilationOptions options)
		{
			var optionsJson = new JObject(
				new JProperty("compress", options.EnableNativeMinification),
				new JProperty("ieCompat", options.IeCompat),
				new JProperty("strictMath", options.StrictMath),
				new JProperty("strictUnits", options.StrictUnits),
				new JProperty("dumpLineNumbers", ConvertLineNumbersModeEnumValueToCode(options.DumpLineNumbers))
			);

			return optionsJson;
		}

		/// <summary>
		/// Converts a line numbers mode enum value to the code
		/// </summary>
		/// <param name="lineNumbersMode">Line numbers mode enum value</param>
		/// <returns>Line numbers mode code</returns>
		private static string ConvertLineNumbersModeEnumValueToCode(LineNumbersMode lineNumbersMode)
		{
			string code;

			switch (lineNumbersMode)
			{
				case LineNumbersMode.None:
					code = string.Empty;
					break;
				case LineNumbersMode.Comments:
					code = "comments";
					break;
				case LineNumbersMode.MediaQuery:
					code = "mediaquery";
					break;
				case LineNumbersMode.All:
					code = "all";
					break;
				default:
					throw new InvalidCastException(string.Format(CoreStrings.Common_EnumValueToCodeConversionFailed,
						lineNumbersMode.ToString(), typeof(LineNumbersMode)));
			}

			return code;
		}

		/// <summary>
		/// Generates a detailed error message
		/// </summary>
		/// <param name="errorDetails">Error details</param>
		/// <param name="sourceCode">Source code</param>
		/// <param name="currentFilePath">Path to current LESS-file</param>
		/// <param name="dependencies">List of dependencies</param>
		/// <returns>Detailed error message</returns>
		private static string FormatErrorDetails(JToken errorDetails, string sourceCode, string currentFilePath,
			DependencyCollection dependencies)
		{
			var type = errorDetails.Value<string>("type");
			var message = errorDetails.Value<string>("message");
			var filePath = errorDetails.Value<string>("fileName");
			var lineNumber = errorDetails.Value<int>("lineNumber");
			var columnNumber = errorDetails.Value<int>("columnNumber");

			string newSourceCode = string.Empty;
			if (string.Equals(filePath, currentFilePath, StringComparison.OrdinalIgnoreCase))
			{
				newSourceCode = sourceCode;
			}
			else
			{
				var dependency = dependencies.GetByUrl(filePath);
				if (dependency != null)
				{
					newSourceCode = dependency.Content;
				}
			}

			string sourceFragment = SourceCodeNavigator.GetSourceFragment(newSourceCode,
				new SourceCodeNodeCoordinates(lineNumber, columnNumber));

			var errorMessage = new StringBuilder();
			if (!string.IsNullOrWhiteSpace(type))
			{
				errorMessage.AppendFormatLine("{0}: {1}", CoreStrings.ErrorDetails_ErrorType, type);
			}
			errorMessage.AppendFormatLine("{0}: {1}", CoreStrings.ErrorDetails_Message, message);
			if (!string.IsNullOrWhiteSpace(filePath))
			{
				errorMessage.AppendFormatLine("{0}: {1}", CoreStrings.ErrorDetails_File, filePath);
			}
			if (lineNumber > 0)
			{
				errorMessage.AppendFormatLine("{0}: {1}", CoreStrings.ErrorDetails_LineNumber,
					lineNumber.ToString());
			}
			if (columnNumber > 0)
			{
				errorMessage.AppendFormatLine("{0}: {1}", CoreStrings.ErrorDetails_ColumnNumber,
					columnNumber.ToString());
			}
			if (!string.IsNullOrWhiteSpace(sourceFragment))
			{
				errorMessage.AppendFormatLine("{1}:{0}{0}{2}", Environment.NewLine,
					CoreStrings.ErrorDetails_SourceError, sourceFragment);
			}

			return errorMessage.ToString();
		}

		/// <summary>
		/// Destroys object
		/// </summary>
		public void Dispose()
		{
			Dispose(true /* disposing */);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Destroys object
		/// </summary>
		/// <param name="disposing">Flag, allowing destruction of 
		/// managed objects contained in fields of class</param>
		private void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				_disposed = true;

				if (_jsEngine != null)
				{
					_jsEngine.Dispose();
				}
			}
		}
	}
}