using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace TelegramEZMod.Services
{
    public class BlocklistService
    {
        private readonly string _blocklistFile = "blocklist.json";
        private BlocklistConfig _blocklistConfig;

        public BlocklistService(long chatId)
        {
            LoadBlocklist(chatId);
        }

        private void LoadBlocklist(long chatId)
        {
            try
            {
                if (File.Exists(_blocklistFile))
                {
                    var jsonContent = File.ReadAllText(_blocklistFile);
                    var blocklists = JsonConvert.DeserializeObject<List<BlocklistConfig>>(jsonContent);
                    _blocklistConfig = blocklists?.FirstOrDefault(b => b.ChatId == chatId) ?? new BlocklistConfig { ChatId = chatId };
                }
                else
                {
                    _blocklistConfig = new BlocklistConfig { ChatId = chatId };
                }
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"JSON Reading Error: {ex.Message}");
                _blocklistConfig = new BlocklistConfig { ChatId = chatId }; // Fallback to default
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                _blocklistConfig = new BlocklistConfig { ChatId = chatId }; // Fallback to default
            }
        }

        private void SaveBlocklist()
        {
            List<BlocklistConfig> blocklists;

            if (File.Exists(_blocklistFile))
            {
                var jsonContent = File.ReadAllText(_blocklistFile);
                blocklists = JsonConvert.DeserializeObject<List<BlocklistConfig>>(jsonContent) ?? new List<BlocklistConfig>();
                var existingBlocklist = blocklists.FirstOrDefault(b => b.ChatId == _blocklistConfig.ChatId);
                if (existingBlocklist != null)
                {
                    blocklists.Remove(existingBlocklist);
                }
            }
            else
            {
                blocklists = new List<BlocklistConfig>();
            }

            blocklists.Add(_blocklistConfig);
            File.WriteAllText(_blocklistFile, JsonConvert.SerializeObject(blocklists, Formatting.Indented));
        }

        public void AddToBlocklist(string word)
        {
            if (!string.IsNullOrWhiteSpace(word) && !_blocklistConfig.BlockedWords.Contains(word))
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

        public void SetBlocklistActive(string isActive)
        {
            _blocklistConfig.BlocklistActive = isActive.Equals("y", StringComparison.OrdinalIgnoreCase) || isActive.Equals("yes", StringComparison.OrdinalIgnoreCase);
            SaveBlocklist();
        }

        public bool IsBlocklistActive() => _blocklistConfig.BlocklistActive;

        public void SetAction(string action)
        {
            if (Enum.TryParse(typeof(BlockAction), action, true, out var parsedAction) || Enum.TryParse(typeof(BlockAction), action, out parsedAction))
            {
                _blocklistConfig.Action = (BlockAction)parsedAction;
                SaveBlocklist();
            }
        }

        public BlockAction GetAction()
        {
            return _blocklistConfig.Action;
        }

        public void SetDeleteMessage(string delete)
        {
            _blocklistConfig.DeleteMessage = delete.Equals("y", StringComparison.OrdinalIgnoreCase) || delete.Equals("yes", StringComparison.OrdinalIgnoreCase);
            SaveBlocklist();
        }

        public bool ShouldDeleteMessage() => _blocklistConfig.DeleteMessage;
    }

    public class BlocklistConfig
    {
        public long ChatId { get; set; }
        public List<string> BlockedWords { get; set; }
        public bool BlocklistActive { get; set; }
        public BlockAction Action { get; set; }
        public bool DeleteMessage { get; set; }
    }

    public enum BlockAction
    {
        Warn = 1,
        Mute = 2,
        Ban = 3
    }
}