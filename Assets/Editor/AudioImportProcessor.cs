using System;
using UnityEditor;
using UnityEngine;

public sealed class AudioImportProcessor : AssetPostprocessor
{
    private const string AudioRoot = "Assets/Audio/Clips/";
    private const string UiAudioFolder = AudioRoot + "UI/";
    private const string GameplayAudioFolder = AudioRoot + "Gameplay/";
    private const string MusicAudioFolder = AudioRoot + "Music/";

    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(AudioRoot, StringComparison.Ordinal))
            return;

        AudioImporter importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.ambisonic = false;

        if (assetPath.StartsWith(UiAudioFolder, StringComparison.Ordinal))
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;
            settings.preloadAudioData = true;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.quality = 1f;
        }
        else if (assetPath.StartsWith(GameplayAudioFolder, StringComparison.Ordinal))
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;
            settings.preloadAudioData = true;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            settings.quality = 1f;
        }
        else if (assetPath.StartsWith(MusicAudioFolder, StringComparison.Ordinal))
        {
            importer.forceToMono = false;
            importer.loadInBackground = true;
            settings.preloadAudioData = false;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
        }
        else
        {
            throw new InvalidOperationException($"Audio asset '{assetPath}' must be inside UI, Gameplay, or Music under '{AudioRoot}'.");
        }

        importer.defaultSampleSettings = settings;
    }
}
