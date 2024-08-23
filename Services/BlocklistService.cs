using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace TelegramBot.Services
{
    public class BlocklistService
    {
        private readonly string _blocklistFile = "blocklist.json";
        private BlocklistConfig _blocklistConfig;

        public BlocklistService()
        {
            LoadBlocklist();
        }

        private void LoadBlocklist()
        {
            try
            {
                if (File.Exists(_blocklistFile))
                {
                    var jsonContent = File.ReadAllText(_blocklistFile);
                    _blocklistConfig = JsonConvert.DeserializeObject<BlocklistConfig>(jsonContent);
                }
                else
                {
                    _blocklistConfig = new BlocklistConfig();
                }
            }
            catch (JsonReaderException ex)
            {
                // Log or handle the exception
                Console.WriteLine($"JSON Reading Error: {ex.Message}");
                _blocklistConfig = new BlocklistConfig(); // Fallback to default
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                Console.WriteLine($"General Error: {ex.Message}");
                _blocklistConfig = new BlocklistConfig(); // Fallback to default
            }
        }

        private void SaveBlocklist()
        {
            File.WriteAllText(_blocklistFile, JsonConvert.SerializeObject(_blocklistConfig, Formatting.Indented));
        }

        public void AddToBlocklist(string word)
        {
            if (!_blocklistConfig.BlockedWords.Contains(word))
            {
                _blocklistConfig.BlockedWords.Add(word);
                SaveBlocklist();
            }
        }

        public void RemoveFromBlocklist(string word)
        {
            if (_blocklistConfig.BlockedWords.Contains(word))
            {
                _blocklistConfig.BlockedWords.Remove(word);
                SaveBlocklist();
            }
        }

        public void ClearBlocklist()
        {
            _blocklistConfig.BlockedWords.Clear();
            SaveBlocklist();
        }

        public List<string> GetBlocklist() => _blocklistConfig.BlockedWords;

        public void SetBlocklistActive(bool isActive)
        {
            _blocklistConfig.BlocklistActive = isActive;
            SaveBlocklist();
        }

        public bool IsBlocklistActive() => _blocklistConfig.BlocklistActive;

        public void SetAction(string action)
        {
            _blocklistConfig.Action = action;
            SaveBlocklist();
        }

        public string GetAction() => _blocklistConfig.Action;

        public void SetDeleteMessage(bool delete)
        {
            _blocklistConfig.DeleteMessage = delete;
            SaveBlocklist();
        }

        public bool ShouldDeleteMessage() => _blocklistConfig.DeleteMessage;
    }

    public class BlocklistConfig
    {
        public List<string> BlockedWords { get; set; } = new List<string>();
        public bool BlocklistActive { get; set; } = false;
        public string Action { get; set; } = "warn";
        public bool DeleteMessage { get; set; } = false;
    }
}