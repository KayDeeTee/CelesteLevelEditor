using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;

namespace CelesteLevelEditor
{
    public class CelesteLevelEditor : EverestModule
    {

        public static CelesteLevelEditor Instance;

        public override Type SettingsType => null;

        public VirtualButton OpenEditor;
        public static Session session;

        public override void Load()
        {
            Everest.Events.Level.OnEnter += Level_OnEnter;
            Everest.Events.Input.OnInitialize += Input_OnInitialize;
            Everest.Events.Input.OnDeregister += Input_OnDeregister;
        }

        public override void Unload()
        {
            Everest.Events.Level.OnEnter -= Level_OnEnter;
            Everest.Events.Input.OnInitialize -= Input_OnInitialize;
            Everest.Events.Input.OnDeregister -= Input_OnDeregister;
        }

        private void Input_OnDeregister()
        {
            OpenEditor?.Deregister();
        }

        private void Input_OnInitialize()
        {
            OpenEditor = new VirtualButton(new CustomKeyboardKey(Keys.F10));
        }

        private void Level_OnEnter(Session session, bool fromSaveData)
        {
            CelesteLevelEditor.session = session;
            
            //VirtualMap<char> vmc = (Celeste.Celeste.Scene as Level).SolidsData;
        }

    }
}
