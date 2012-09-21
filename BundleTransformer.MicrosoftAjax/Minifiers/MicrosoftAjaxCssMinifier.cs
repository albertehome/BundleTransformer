﻿namespace BundleTransformer.MicrosoftAjax.Minifiers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Microsoft.Ajax.Utilities;
	using MsCssColor = Microsoft.Ajax.Utilities.CssColor;
	using MsCssComment = Microsoft.Ajax.Utilities.CssComment;
	using MsOutputMode = Microsoft.Ajax.Utilities.OutputMode;
	using MsBlockStart = Microsoft.Ajax.Utilities.BlockStart;

	using Core;
	using Core.Assets;
	using Core.Minifiers;
	using CoreStrings = Core.Resources.Strings;

	using Configuration;
	using BtCssColor = CssColor;
	using BtCssComment = CssComment;
	using BtOutputMode = OutputMode;
	using BtBlockStart = BlockStart;

	/// <summary>
	/// Minifier, which produces minifiction of CSS-code 
	/// by using Microsoft Ajax Minifier
	/// </summary>
	public sealed class MicrosoftAjaxCssMinifier : MicrosoftAjaxMinifierBase
	{
		/// <summary>
		/// Name of minifier
		/// </summary>
		const string MINIFIER_NAME = "Microsoft Ajax CSS-minifier";

		/// <summary>
		/// Name of code type
		/// </summary>
		const string CODE_TYPE = "CSS";

		/// <summary>
		/// CSS-parser
		/// </summary>
		private readonly CssParser _cssParser;

		/// <summary>
		/// Configuration settings of CSS-parser
		/// </summary>
		private readonly CssSettings _cssParserConfiguration;

		/// <summary>
		/// Gets or sets whether embedded ASP.NET blocks (&lt;% %gt;) 
		/// should be recognized and output as is
		/// </summary>
		public override bool AllowEmbeddedAspNetBlocks
		{
			get
			{
				return _cssParserConfiguration.AllowEmbeddedAspNetBlocks;
			}
			set
			{
				_cssParserConfiguration.AllowEmbeddedAspNetBlocks = value;
			}
		}

		/// <summary>
		/// Gets or sets ColorNames setting
		/// </summary>
		public BtCssColor ColorNames
		{
			get
			{
				return Utils.GetEnumFromOtherEnum<MsCssColor, BtCssColor>(_cssParserConfiguration.ColorNames);
			}
			set
			{
				_cssParserConfiguration.ColorNames = Utils.GetEnumFromOtherEnum<BtCssColor, MsCssColor>(value);
			}
		}

		/// <summary>
		/// Gets or sets CommentMode setting
		/// </summary>
		public BtCssComment CommentMode
		{
			get
			{
				return Utils.GetEnumFromOtherEnum<MsCssComment, BtCssComment>(_cssParserConfiguration.CommentMode);
			}
			set
			{
				_cssParserConfiguration.CommentMode = Utils.GetEnumFromOtherEnum<BtCssComment, MsCssComment>(value);
			}
		}

		/// <summary>
		/// Gets or sets a string representation of the list of 
		/// debug lookups, comma-separated
		/// </summary>
		public override string IgnoreErrorList
		{
			get
			{
				return _cssParserConfiguration.IgnoreErrorList;
			}
			set
			{
				_cssParserConfiguration.IgnoreErrorList = value;
			}
		}

		/// <summary>
		/// Gets or sets number of spaces per indent level when in 
		/// MultipleLines output mode
		/// </summary>
		public override int IndentSize
		{
			get
			{
				return _cssParserConfiguration.IndentSize;
			}
			set
			{
				_cssParserConfiguration.IndentSize = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to minify the 
		/// JavaScript within expression functions
		/// </summary>
		public bool MinifyExpressions
		{
			get
			{
				return _cssParserConfiguration.MinifyExpressions;
			}
			set
			{
				_cssParserConfiguration.MinifyExpressions = value;
			}
		}

		/// <summary>
		/// Gets or sets a output mode:
		/// SingleLine - output all code on a single line;
		/// MultipleLines - break the output into multiple lines to be more human-readable
		/// </summary>
		public override BtOutputMode OutputMode
		{
			get
			{
				return Utils.GetEnumFromOtherEnum<MsOutputMode, BtOutputMode>(_cssParserConfiguration.OutputMode);
			}
			set
			{
				_cssParserConfiguration.OutputMode = Utils.GetEnumFromOtherEnum<BtOutputMode, MsOutputMode>(value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the opening curly brace for blocks is
		/// on its own line (NewLine, default) or on the same line as the preceding code (SameLine)
		/// or taking a hint from the source code position (UseSource). Only relevant when OutputMode is 
		/// set to MultipleLines.
		/// </summary>
		public override BtBlockStart BlocksStartOnSameLine
		{
			get
			{
				return Utils.GetEnumFromOtherEnum<MsBlockStart, BtBlockStart>(_cssParserConfiguration.BlocksStartOnSameLine);
			}
			set 
			{ 
				_cssParserConfiguration.BlocksStartOnSameLine = Utils.GetEnumFromOtherEnum<BtBlockStart, MsBlockStart>(value);
			}
		}

		/// <summary>
		/// Gets or sets string representation of the list 
		/// of names defined for the preprocessor, comma-separated
		/// </summary>
		public override string PreprocessorDefineList
		{
			get
			{
				return _cssParserConfiguration.PreprocessorDefineList;
			}
			set
			{
				_cssParserConfiguration.PreprocessorDefineList = value;
			}
		}

		/// <summary>
		/// Gets or sets a flag for whether to add a semicolon 
		/// at the end of the parsed code
		/// </summary>
		public override bool TermSemicolons
		{
			get
			{
				return _cssParserConfiguration.TermSemicolons;
			}
			set
			{
				_cssParserConfiguration.TermSemicolons = value;
			}
		}


		/// <summary>
		/// Constructs instance of Microsoft Ajax CSS-minifier
		/// </summary>
		public MicrosoftAjaxCssMinifier()
			: this(BundleTransformerContext.Current.GetMicrosoftAjaxConfiguration())
		{ }

		/// <summary>
		/// Constructs instance of Microsoft Ajax CSS-minifier
		/// </summary>
		/// <param name="microsoftAjaxConfig">Configuration settings of Microsoft Ajax Minifier</param>
		public MicrosoftAjaxCssMinifier(MicrosoftAjaxSettings microsoftAjaxConfig)
		{
			_cssParser = new CssParser();
			_cssParser.CssError += ParserErrorHandler;

			_cssParserConfiguration = _cssParser.Settings;

			CssMinifierSettings cssMinifierConfig = microsoftAjaxConfig.CssMinifier;
			AllowEmbeddedAspNetBlocks = cssMinifierConfig.AllowEmbeddedAspNetBlocks;
			ColorNames = cssMinifierConfig.ColorNames;
			CommentMode = cssMinifierConfig.CommentMode;
			IgnoreErrorList = cssMinifierConfig.IgnoreErrorList;
			IndentSize = cssMinifierConfig.IndentSize;
			MinifyExpressions = cssMinifierConfig.MinifyExpressions;
			OutputMode = cssMinifierConfig.OutputMode;
			BlocksStartOnSameLine = cssMinifierConfig.BlocksStartOnSameLine;
			PreprocessorDefineList = cssMinifierConfig.PreprocessorDefineList;
			TermSemicolons = cssMinifierConfig.TermSemicolons;
			Severity = cssMinifierConfig.Severity;
		}


		/// <summary>
		/// Produces code minifiction of CSS-assets by using Microsoft Ajax Minifier
		/// </summary>
		/// <param name="assets">Set of CSS-assets</param>
		/// <returns>Set of CSS-assets with minified text content</returns>
		public override IList<IAsset> Minify(IList<IAsset> assets)
		{
			if (assets == null)
			{
				throw new ArgumentException(CoreStrings.Common_ValueIsEmpty, "assets");
			}

			if (assets.Count == 0)
			{
				return assets;
			}

			var assetsToProcessing = assets.Where(a => a.IsStylesheet && !a.Minified).ToList();
			if (assetsToProcessing.Count == 0)
			{
				return assets;
			}

			foreach (var asset in assetsToProcessing)
			{
				string newContent;
				string assetPath = asset.Path;

				_cssParser.FileContext = assetPath;

				try
				{
					newContent = _cssParser.Parse(asset.Content);
				}
				catch (Exception e)
				{
					throw new AssetMinificationException(
						string.Format(CoreStrings.Minifiers_MinificationFailed, 
							CODE_TYPE, assetPath, MINIFIER_NAME, e.Message), e);
				}
				finally
				{
					_cssParser.FileContext = null;
				}

				asset.Content = newContent;
				asset.Minified = true;
			}

			return assets;
		}

		/// <summary>
		/// CSS-parser error handler
		/// </summary>
		/// <param name="source">The source of the event</param>
		/// <param name="args">A Microsoft.Ajax.Utilities.CssErrorEventArgs 
		/// that contains the event data</param>
		private void ParserErrorHandler(object source, CssErrorEventArgs args)
		{
			ContextError error = args.Error;

			if (error.Severity <= Severity)
			{
				throw new MicrosoftAjaxParsingException(
					string.Format(CoreStrings.Minifiers_MinificationSyntaxError, 
						CODE_TYPE, error.File, MINIFIER_NAME, FormatContextError(error)), args.Exception);
			}
		}
	}
}