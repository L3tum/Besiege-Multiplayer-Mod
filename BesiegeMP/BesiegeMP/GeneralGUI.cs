using System;
using System.IO;
using spaar.ModLoader;
using spaar.ModLoader.UI;
using UnityEngine;

namespace BesiegeMP
{
    class GeneralGUI : MonoBehaviour
    {
        private int _winID = spaar.ModLoader.Util.GetWindowID();
        private int _helpID = spaar.ModLoader.Util.GetWindowID();
        private Rect _winRect = new Rect(100.0f, 100.0f, 500.0f, 400.0f);
        public Texture2D text;
        private GUIStyle _toggleStyle, _labelStyle, _headlineStyle, _defaultLabel, _windowStyle;
        private bool show, stylesDone;
        private String lastTooltip = "";
        private readonly Key showKey = spaar.ModLoader.Keybindings.AddKeybinding("Multiplayer", new Key(KeyCode.LeftControl, KeyCode.B));

        public void Start()
        {
            text = new Texture2D(0, 0);
            var bytes = File.ReadAllBytes(Application.dataPath + "/Mods/Scripts/Resource/UI/background.png");
            text.LoadImage(bytes);
        }

        public void OnGUI()
        {
            if (ModGUI.Skin == null)
            {
                return;
            }
            GUI.skin = ModGUI.Skin;
            if (!stylesDone)
            {
                _windowStyle = new GUIStyle(GUI.skin.window)
                {
                    normal = new GUIStyleState()
                    {
                        background = text,
                        textColor = GUI.skin.window.normal.textColor
                    },
                };
                _toggleStyle = new GUIStyle(GUI.skin.button) {onNormal = Elements.Buttons.Red.hover};
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    margin =
                        new RectOffset(GUI.skin.textField.margin.left, GUI.skin.textField.margin.right, GUI.skin.textField.margin.top, GUI.skin.textField.margin.bottom),
                    border = GUI.skin.textField.border,
                    alignment = TextAnchor.LowerRight,
                };
                _headlineStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft,
                    border = _labelStyle.border,
                    fontStyle = FontStyle.Bold
                };
                stylesDone = true;
            }


            if (show)
            {
                _winRect = GUI.Window(_winID, _winRect, WinFunc, "Multiplayer Menu", _windowStyle);
            }
            if (lastTooltip != "")
            {
                GUI.skin = ModGUI.Skin;
                var background = GUI.skin.label.normal.background;
                _defaultLabel = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    normal = new GUIStyleState {background = background, textColor = Color.white}
                };
                GUILayout.Window(_helpID, new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y + 10.0f, 100.0f, 50.0f), HelpFunc, "ToolTip", _windowStyle);
            }
        }

        private void WinFunc(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Name:", "Your name. Will be visible in-game"), _labelStyle, GUILayout.Width(200.0f));
            GUILayout.Space(25.0f);
            Settings.Name = GUILayout.TextField(Settings.Name);
            GUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Location:", "The Location of you, either Server or Client. Will be used to enhance match searching"), _labelStyle, GUILayout.Width(200.0f));
            GUILayout.Space(25.0f);
            Settings.Location = GUILayout.TextField(Settings.Location);
            GUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Server Name:", "The name the server will be visible in the matchmaking list. Choose a descriptive, unique name"), _labelStyle, GUILayout.Width(200.0f));
            GUILayout.Space(25.0f);
            Settings.ServerName = GUILayout.TextField(Settings.ServerName);
            GUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Port:", "The port the Multiplayer will use. Leave this on default if you're not planning on hosting/joining multiple games"), _labelStyle, GUILayout.Width(200.0f));
            GUILayout.Space(25.0f);
            Settings.ServerPort =
                Int32.Parse(GUILayout.TextField(Settings.ServerPort.ToString()));
            GUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Server Password:", "Password of the Server. Leave it empty if anyone can connect"), _labelStyle, GUILayout.Width(200.0f));
            GUILayout.Space(25.0f);
            Settings.ServerPassword = GUILayout.TextField(Settings.ServerPassword);
            GUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent("Max Players:", "The maximum amount of players on your server. If normal Server this number includes the host"), _labelStyle, GUILayout.Width(200.0f));
            GUILayout.Space(25.0f);
            Settings.maxPlayers =
                Int32.Parse(GUILayout.TextField(Settings.maxPlayers.ToString()));
            GUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Space(50.0f);
            Settings.getLocation = GUILayout.Toggle(Settings.getLocation, new GUIContent("Get Location", "Toggles the Usage of an API to determine your location for better matchmaking"), _toggleStyle, GUILayout.Width(200.0f));
            GUILayout.Space(50.0f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10.0f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Start Dedicated Server", "Starts a dedicated Server")))
            {
                gameObject.AddComponent<DedicatedServer>();
            }
            if (GUILayout.Button(new GUIContent("Start Server", "Starts a normal Server with you as host")))
            {
                gameObject.AddComponent<Server>();
            }
            if (GUILayout.Button(new GUIContent("Client", "Starts the Client to connect to Servers")))
            {
                gameObject.AddComponent<Client>();
            }
            GUILayout.EndHorizontal();

            lastTooltip = GUI.tooltip;
            GUI.DragWindow();
        }

        public void Update()
        {
            if (showKey.Pressed())
            {
                show = !show;
            }
        }

        private void HelpFunc(int id)
        {
            GUILayout.Label(lastTooltip, _defaultLabel);
        }
    }
}