using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using spaar.ModLoader.UI;
using UnityEngine;

namespace BesiegeMP
{
    class GeneralGUI : MonoBehaviour
    {
        private int _winID = spaar.ModLoader.Util.GetWindowID();
        private Rect _winRect = new Rect(100.0f, 100.0f, 200.0f, 400.0f);
        private Texture2D text;
        private GUIStyle _toggleStyle, _labelStyle, _headlineStyle;

        public void Start()
        {
            text = new Texture2D(0, 0);
            var bytes = File.ReadAllBytes(Application.dataPath + "/Mods/Scripts/Resource/UI/background.png");
            text.LoadImage(bytes);
        }

        public void OnGUI()
        {
            GUI.skin = ModGUI.Skin;
            GUI.skin.window.normal.background = text;
            GUI.skin.textArea.richText = true;
            _toggleStyle = new GUIStyle(GUI.skin.button) { onNormal = Elements.Buttons.Red.hover };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                margin =
                    new RectOffset(10, 5, GUI.skin.textField.margin.top, GUI.skin.textField.margin.bottom),
                border = GUI.skin.textField.border,
                alignment = GUI.skin.textField.alignment
            };
            _headlineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                border = _labelStyle.border,
                fontStyle = FontStyle.Bold
            };
            _winRect = GUI.Window(_winID, _winRect, WinFunc, "Multiplayer Menu");
        }

        private void WinFunc(int id)
        {
                    
        }
    }
}
