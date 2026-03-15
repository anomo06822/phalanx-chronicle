using System;
using System.IO;
using System.Linq;
using PhalanxChronicle.Battle;
using PhalanxChronicle.Core;
using UnityEngine;
using Xunit;

namespace PhalanxChronicle.Headless.Tests
{
    public sealed class CampaignSaveRepositoryTests
    {
        [Fact]
        public void GetSlotSummaries_ReturnsThreeEmptySlotsWithExpectedPaths()
        {
            using TempPersistentDataScope scope = new TempPersistentDataScope();
            CampaignProgressionService service = new CampaignProgressionService();

            IReadOnlyList<CampaignSaveSlotSummary> summaries = CampaignSaveRepository.GetSlotSummaries(
                service,
                CampaignCatalog.LiuBeiLegendCampaignId);

            Assert.Equal(CampaignSaveRepository.SlotCount, summaries.Count);
            Assert.All(summaries, summary =>
            {
                Assert.False(summary.HasSave);
                Assert.Null(summary.SaveData);
                Assert.Equal(
                    Path.Combine(scope.DirectoryPath, $"phalanx-chronicle-save-slot{summary.SlotIndex}.json"),
                    summary.FilePath);
            });
        }

        [Fact]
        public void CreateForSlot_PersistsOnlyToTheSelectedSlot()
        {
            using TempPersistentDataScope scope = new TempPersistentDataScope();
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignDefinition definition = CampaignCatalog.CreateLiuBeiLegend();

            CampaignSaveRepository slotTwoRepository = CampaignSaveRepository.CreateForSlot(service, 2);
            CampaignSaveData save = slotTwoRepository.CreateNew(definition);

            IReadOnlyList<CampaignSaveSlotSummary> summaries = CampaignSaveRepository.GetSlotSummaries(service, definition.CampaignId);

            Assert.NotNull(save);
            Assert.False(File.Exists(CampaignSaveRepository.GetSlotSavePath(1)));
            Assert.True(File.Exists(CampaignSaveRepository.GetSlotSavePath(2)));
            Assert.False(File.Exists(CampaignSaveRepository.GetSlotSavePath(3)));
            Assert.False(summaries.Single(summary => summary.SlotIndex == 1).HasSave);
            Assert.True(summaries.Single(summary => summary.SlotIndex == 2).HasSave);
            Assert.False(summaries.Single(summary => summary.SlotIndex == 3).HasSave);
        }

        [Fact]
        public void TryMigrateLegacySaveToSlotOne_MovesLegacyFileWhenSlotOneIsEmpty()
        {
            using TempPersistentDataScope scope = new TempPersistentDataScope();
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignDefinition definition = CampaignCatalog.CreateLiuBeiLegend();
            CampaignSaveRepository legacyRepository = new CampaignSaveRepository(service);

            legacyRepository.CreateNew(definition);

            bool migrated = CampaignSaveRepository.TryMigrateLegacySaveToSlotOne();
            CampaignSaveRepository slotOneRepository = CampaignSaveRepository.CreateForSlot(service, 1);

            Assert.True(migrated);
            Assert.False(File.Exists(legacyRepository.SavePath));
            Assert.True(File.Exists(slotOneRepository.SavePath));
            Assert.True(slotOneRepository.TryLoad(definition.CampaignId, out CampaignSaveData migratedSave));
            Assert.NotNull(migratedSave);
        }

        [Fact]
        public void TryMigrateLegacySaveToSlotOne_DoesNotOverwriteExistingSlotOneSave()
        {
            using TempPersistentDataScope scope = new TempPersistentDataScope();
            CampaignProgressionService service = new CampaignProgressionService();
            CampaignDefinition definition = CampaignCatalog.CreateLiuBeiLegend();
            CampaignSaveRepository slotOneRepository = CampaignSaveRepository.CreateForSlot(service, 1);
            CampaignSaveRepository legacyRepository = new CampaignSaveRepository(service);

            slotOneRepository.CreateNew(definition);
            legacyRepository.CreateNew(definition);

            bool migrated = CampaignSaveRepository.TryMigrateLegacySaveToSlotOne();

            Assert.False(migrated);
            Assert.True(File.Exists(slotOneRepository.SavePath));
            Assert.True(File.Exists(legacyRepository.SavePath));
        }

        private sealed class TempPersistentDataScope : IDisposable
        {
            private readonly string originalPersistentDataPath;

            public TempPersistentDataScope()
            {
                originalPersistentDataPath = Application.persistentDataPath;
                DirectoryPath = Path.Combine(Path.GetTempPath(), "phalanx-chronicle-tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(DirectoryPath);
                Application.persistentDataPath = DirectoryPath;
            }

            public string DirectoryPath { get; }

            public void Dispose()
            {
                Application.persistentDataPath = originalPersistentDataPath;
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, true);
                }
            }
        }
    }
}
