﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace TagCloud.TextProcessing
{
    internal class TextProcessor : ITextProcessor
    {
        private readonly IFileReader _textReader;
        private const string UtilFileName = "mystem.exe";
        private const string TempWritePath = @"c:\temp\input.txt";
        private const string TempPath = @"c:\temp\output.txt";
        private const string Arguments = "-nl -ig -d --format json";

        public TextProcessor(IFileReader textReader)
        {
            _textReader = textReader;
        }
        
        public IEnumerable<Dictionary<string, int>> GetWordsWithFrequency(ITextProcessingOptions options)
        {
            foreach (var filePath in options.FilesToProcess)
            {
                using var writer = new StreamWriter(TempWritePath, false, Encoding.UTF8);
                writer.Write(_textReader.ReadFile(filePath));
                using (var process = ConfigureProcess(TempWritePath))
                {
                    process.Start();                    
                    process.WaitForExit();  
                }

                var myStemResults = ParseMyStemResult();
                File.Delete(TempPath);

                yield return myStemResults
                    .Where(r => !options.ExcludePartOfSpeech.Contains(r.PartOfSpeech)
                                || options.IncludeWords.Contains(r.Lemma))
                    .Select(r => r.Lemma)
                    .Where(w => !options.ExcludeWords.Contains(w))
                    .GroupBy(s => s)
                    .OrderByDescending(g => g.Count())
                    .Take(options.Amount)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }

        private static IEnumerable<MyStemResult?> ParseMyStemResult()
        {
            return File.ReadAllText(TempPath)
                .Split("\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(JsonConvert.DeserializeObject<MyStemResult>)
                .Where(r => r?.analysis.Count > 0);
        }

        private static Process ConfigureProcess(string filepath)
        {
            var process = new Process();
            process.StartInfo.FileName = UtilFileName;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = $"{Arguments} {filepath} {TempPath}";
            
            return process;
        }
    }
}