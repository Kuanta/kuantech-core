namespace Kuantech.Core.FX
{
    /// <summary>
    /// A minimal submanager that plays a song when scene starts
    /// </summary>
    public class SceneMusicHandler : SubManager
    {
        public Music MusicToPlay;
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            AudioLibrary audioLibrary = EffectsLibrary.GetAudioLibrary();
            if (audioLibrary == null) return;
            if (MusicToPlay == null) return;
            audioLibrary.PlayMusic(MusicToPlay);
        }
        
    }
}