using Boo.Lang;
using Boo.Lang.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityScript.Lang;

[Serializable]
public class AddPiece : SingleInstance<AddPiece>
{
    [NonSerialized]
    public static bool usingCopiedBlock;

    [NonSerialized]
    public static XDataHolder copiedBlockData;

    private int _currentBlockType;

    public BlockMenuControl[] blockMenuControl;

    [NonSerialized]
    public static bool eraseMode;

    [NonSerialized]
    public static bool keyMapMode;

    [NonSerialized]
    public static bool selectSymmetryPivot;

    [NonSerialized]
    public static bool disableBlockPlacement;

    [NonSerialized]
    public static bool disableBlockHighlight;

    [NonSerialized]
    public static Vector3 mouseHitPos;

    [NonSerialized]
    public static Vector3 mouseHitNormal;

    [NonSerialized]
    public static Quaternion blockPlacedRotation;

    [NonSerialized]
    public static Vector3 hammerPos;

    [NonSerialized]
    public static Vector3 hammerFwd;

    [NonSerialized]
    public static bool individualOutOfBounds;

    [NonSerialized]
    public static bool ghostEnabled;

    [NonSerialized]
    public static float timeTaken;

    public RaycastHit mouseHit;

    public bool mouseHasHit;

    public RaycastHit hudHit;

    public bool checkPinBlocks;

    public Camera mainCam;

    public Camera hudCam;

    public Transform ghostUnknown;

    public LayerMask layerMasky;

    public LayerMask layerMaskyHud;

    [HideInInspector]
    public bool canAdd;

    public AudioSource clickSound;

    public bool npcBlock;

    public int solverIterationCounty;

    public Vector3 rotator;

    public Transform middleOfObject;

    public Transform activeInstance;

    public int woodBlockSolverIterationCount;

    public float rotationAmount;

    public BarPositionController barPosCode;

    public bool outOfBounds;

    public float xBounds;

    public float yBounds;

    public float zBounds;

    public PlayButton playButton;

    public TextMesh massTextMesh;

    public TextMesh blockCountTextMesh;

    public float camSmoothSpeed;

    public Transform physicsGoalObject;

    public List<int> lastAction;

    public float nailCutoffHeight;

    public Bounds machineColliderBounds;

    public BoundingBoxController boundVisCode;

    public OutOfBoundsWarning OutOfBoundsWarningCode;

    public GodToolsWarning GodToolsWarningCode;

    public RandomSoundController deleteSound;

    public EraseButton eraseButtonCode;

    public KeyMapModeButton keyMapButtonCode;

    public List<Transform> sendSimulateMessage;

    public MachineCenterOfMass comCode;

    public RandomSoundController woodHitAudioController;

    public SymmetryController symmetryController;

    [HideInInspector]
    public bool hudOccluding;

    [HideInInspector]
    public float floorHeight;

    private Ray ray;

    private Ray rayHud;

    private MachineObjectTracker machineTracker;

    private GenericBlock _hoveredBlock;

    [NonSerialized]
    public static GenericBlock SelectedBlock;

    private GhostMaterialController ghostUnknownMaterialController;

    private Transform activeGhost;

    private float deleteDownTime;

    [HideInInspector]
    [NonSerialized]
    public static bool canSimulate;

    private Transform lastObject;

    private BlockBehaviour lastBlock;

    private Vector3 lastMiddleOfObject;

    private Transform _currentGhost;

    private GhostMaterialController _currentGhostController;

    private GhostTrigger _currentGhostTrigger;

    private Transform _currentHammerObj;

    private MyBlockInfo _currentGhostBlockInfo;

    private bool _currentGhostFlipped;

    private Transform _currentGhostArrow;

    private LayerMask pinMask;

    private float enterTimer;

    public override int BlockType
    {
        get
        {
            return this._currentBlockType;
        }
    }

    public override Transform CurrentGhost
    {
        get
        {
            return this._currentGhost;
        }
    }

    public override Transform CurrentGhostArrow
    {
        get
        {
            return this._currentGhostArrow;
        }
    }

    public override GenericBlock HoveredBlock
    {
        get
        {
            return this._hoveredBlock;
        }
        set
        {
            if (this._hoveredBlock != value)
            {
                this._hoveredBlock = value;
            }
        }
    }

    public override BlockBehaviour LastBlock
    {
        get
        {
            return this.lastBlock;
        }
    }

    public override string Name
    {
        get
        {
            return "_BUILDER";
        }
    }

    static AddPiece()
    {
        AddPiece.canSimulate = true;
    }

    public AddPiece()
    {
        this.solverIterationCounty = 12;
        this.woodBlockSolverIterationCount = 50;
        this.xBounds = (float)100;
        this.yBounds = (float)100;
        this.zBounds = (float)100;
        this.camSmoothSpeed = (float)6;
        this.lastAction = new List<int>();
        this.nailCutoffHeight = 1f;
        this.sendSimulateMessage = new List<Transform>();
    }

    public override void AddBlock(Transform block)
    {
        Vector3 vector3 = block.position;
        Quaternion quaternion = block.rotation;
        this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(vector3, quaternion, this.BlockType);
        this.lastObject = this.lastBlock.transform;
        if (this._currentGhostFlipped && block == this.activeGhost)
        {
            this.lastBlock.Flipped = this._currentGhostFlipped;
        }
        else if (this._currentGhostArrow && this.BlockType != 13 && this.BlockType != 28)
        {
            if (this.CompareVectors(block.forward, -Vector3.right, (float)45) == this._currentGhostFlipped)
            {
                this.lastBlock.Flipped = this._currentGhostFlipped;
            }
            else
            {
                this.lastBlock.Flipped = !this._currentGhostFlipped;
            }
        }
        this.UpdateMiddleOfObject();
    }

    public override void AddBlockType(Transform block)
    {
        if (this.AddBlockTypeNoSound(block))
        {
            this.woodHitAudioController.Stop();
            this.woodHitAudioController.Play();
            this._currentGhostController.SetNormal();
        }
    }

    public override bool AddBlockTypeNoSound(Transform block)
    {
        bool flag;
        if ((!AddPiece.individualOutOfBounds || StatMaster.disabledBlockIntersectionWarning) && !(this.activeGhost == null) && this.BlockType != 0)
        {
            bool flag1 = false;
            if (this.BlockType == 19)
            {
                this.AddHinge(block);
                flag1 = true;
            }
            else if (this.BlockType == 7)
            {
                this.AddBrace(block);
                flag1 = false;
            }
            else if (this.BlockType == 9 || this.BlockType == 45)
            {
                this.AddSpring(block);
                flag1 = false;
            }
            else if (this.BlockType == 5)
            {
                this.AddHinge(block);
                flag1 = true;
            }
            else if (this.BlockType != 8)
            {
                this.AddBlock(block);
                flag1 = true;
            }
            else
            {
                Debug.Log("Tried loading block id 8, unused block known to cause issues, and interfere with modding, refrained from loading block.");
                flag1 = false;
            }
            if (this.lastObject.GetComponent<MyBounds>())
            {
                this.lastObject.GetComponent<MyBounds>().GetAll();
            }
            if (flag1)
            {
                Machine.Active().UndoSystem.Snapshot();
            }
            flag = true;
        }
        else
        {
            flag = false;
        }
        return flag;
    }

    public override void AddBrace(Transform block)
    {
        this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(block.position, block.rotation, this.BlockType);
        this.lastObject = this.lastBlock.transform;
        this.lastObject.gameObject.SendMessage("setJointTo", this.mouseHit.rigidbody, SendMessageOptions.DontRequireReceiver);
    }

    public override void AddHinge(Transform block)
    {
        this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(block.position, block.rotation, this.BlockType);
        this.lastObject = this.lastBlock.transform;
        this.lastObject.GetComponent<HingeJoint>();
    }

    public override void AddSpring(Transform block)
    {
        this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(block.position, block.rotation, this.BlockType);
        this.lastObject = this.lastBlock.transform;
    }

    protected override void Awake()
    {
        this.machineTracker = Object.FindObjectOfType<MachineObjectTracker>();
        Physics.defaultSolverIterations = this.solverIterationCounty;
        this.physicsGoalObject = GameObject.Find("PHYSICS GOAL").transform;
        StatMaster.isSimulating = false;
        StatMaster.wasSimulating = false;
        AddPiece.eraseMode = false;
        AddPiece.keyMapMode = false;
        AddPiece.selectSymmetryPivot = false;
        AddPiece.disableBlockPlacement = false;
        AddPiece.individualOutOfBounds = false;
        AddPiece.ghostEnabled = false;
        this.pinMask = AddPiece.CreateLayerMask(new int[] { 27 });
    }

    private void BlockDeselect()
    {
        if (AddPiece.SelectedBlock != null)
        {
            BlockVisualController visualController = null;
            if (AddPiece.SelectedBlock != null)
            {
                visualController = AddPiece.SelectedBlock.VisualController;
                if (visualController != null)
                {
                    visualController.SetNormal();
                }
            }
            AddPiece.SelectedBlock = null;
        }
    }

    private void BlockHoverOut()
    {
        if (this._hoveredBlock != AddPiece.SelectedBlock)
        {
            if (this._hoveredBlock != null)
            {
                if (AddPiece.eraseMode || AddPiece.keyMapMode || AddPiece.selectSymmetryPivot || AddPiece.disableBlockPlacement)
                {
                    BlockVisualController visualController = this._hoveredBlock.VisualController;
                    if (visualController != null)
                    {
                        visualController.SetNormal();
                    }
                }
                this._hoveredBlock = null;
            }
        }
    }

    private void BlockHoverOver(BlockBehaviour block)
    {
        if (block != AddPiece.SelectedBlock)
        {
            if (this._hoveredBlock != block)
            {
                if (this.HoveredBlock != null)
                {
                    this.BlockHoverOut();
                }
                this._hoveredBlock = (GenericBlock)block;
                if (!AddPiece.disableBlockHighlight && !AddPiece.selectSymmetryPivot)
                {
                    if (AddPiece.eraseMode || AddPiece.keyMapMode || AddPiece.disableBlockPlacement)
                    {
                        BlockVisualController visualController = block.VisualController;
                        if (visualController != null)
                        {
                            visualController.SetHighlighted();
                        }
                    }
                }
            }
        }
    }

    private void BlockSelect(BlockBehaviour block)
    {
        if (block != null)
        {
            if (block != AddPiece.SelectedBlock)
            {
                BlockVisualController visualController = null;
                if (AddPiece.SelectedBlock != null)
                {
                    visualController = AddPiece.SelectedBlock.VisualController;
                    if (visualController != null)
                    {
                        visualController.SetNormal();
                    }
                }
                AddPiece.SelectedBlock = (GenericBlock)block;
                if (!AddPiece.disableBlockHighlight && !AddPiece.selectSymmetryPivot)
                {
                    visualController = this._hoveredBlock.VisualController;
                    if (visualController != null)
                    {
                        visualController.SetSelected();
                    }
                }
            }
        }
    }

    public static AddPiece.DirAnglePair CompareDirAndAxis(Vector3 dir, Vector3 axis)
    {
        AddPiece.DirAnglePair dirAnglePair = new AddPiece.DirAnglePair()
        {
            angle = Vector3.Angle(dir, axis),
            dir = dir
        };
        return dirAnglePair;
    }

    public override bool CompareVectors(Vector3 a, Vector3 b, float angleError)
    {
        bool flag;
        if (Mathf.Approximately(a.magnitude, b.magnitude))
        {
            float single = Mathf.Cos(angleError * 0.0174532924f);
            float single1 = Vector3.Dot(a.normalized, b.normalized);
            flag = single1 >= single;
        }
        else
        {
            flag = false;
        }
        return flag;
    }

    public static LayerMask CreateLayerMask(int[] layers)
    {
        LayerMask layerMask = 0;
        for (int i = 0; i < layers.Length; i++)
        {
            layerMask = layerMask.@value + (1 << layers[i]);
        }
        return layerMask;
    }

    public static LayerMask CreateLayerMask(LayerMask mask, int[] layers)
    {
        LayerMask layerMask = mask.@value;
        for (int i = 0; i < layers.Length; i++)
        {
            layerMask = layerMask.@value + (1 << layers[i]);
        }
        return layerMask;
    }

    public static Vector3 DirWithSmallestAngle(AddPiece.DirAnglePair[] values)
    {
        float single = 360f;
        Vector3 vector3 = Vector3.one;
        int num = 0;
        AddPiece.DirAnglePair[] dirAnglePairArray = values;
        int length = dirAnglePairArray.Length;
        while (num < length)
        {
            if (dirAnglePairArray[num].angle < single)
            {
                single = dirAnglePairArray[num].angle;
                vector3 = dirAnglePairArray[num].dir;
            }
            num++;
        }
        return vector3;
    }

    public override void FinishAddingBrace(Transform blocky)
    {
        Machine.Active().UndoSystem.Snapshot();
    }

    public static Vector3 GetLocalDirClosestTo(Transform t, Vector3 axis)
    {
        AddPiece.DirAnglePair dirAnglePair = AddPiece.CompareDirAndAxis(t.forward, axis);
        AddPiece.DirAnglePair dirAnglePair1 = AddPiece.CompareDirAndAxis(t.up, axis);
        AddPiece.DirAnglePair dirAnglePair2 = AddPiece.CompareDirAndAxis(t.right, axis);
        AddPiece.DirAnglePair dirAnglePair3 = AddPiece.CompareDirAndAxis(-t.forward, axis);
        AddPiece.DirAnglePair dirAnglePair4 = AddPiece.CompareDirAndAxis(-t.up, axis);
        AddPiece.DirAnglePair dirAnglePair5 = AddPiece.CompareDirAndAxis(-t.right, axis);
        return AddPiece.DirWithSmallestAngle(new AddPiece.DirAnglePair[] { dirAnglePair, dirAnglePair1, dirAnglePair2, dirAnglePair3, dirAnglePair4, dirAnglePair5 });
    }

    public override MyBlockInfo GetSelectedBlockInfo()
    {
        MyBlockInfo myBlockInfo;
        GenericBlock genericBlock = UnityRuntimeServices.Invoke(SingleInstance<MachineConstructor>.Instance, "GetBlock", new object[] { this.BlockType }, typeof(MonoBehaviour)) as GenericBlock;
        if (genericBlock != null)
        {
            myBlockInfo = genericBlock.MyBlockInfo;
        }
        else
        {
            Debug.LogWarning("There's no block with the ID #" + this.BlockType);
            this.SetBlockType(this.BlockType - 1);
            if (this.BlockType >= 0)
            {
                myBlockInfo = this.GetSelectedBlockInfo();
            }
            else
            {
                myBlockInfo = null;
            }
        }
        return myBlockInfo;
    }

    public override IEnumerator RemoveBlock()
    {
        return (new AddPiece.$RemoveBlock$2203(this)).GetEnumerator();
    }

    public override void SendSimulateMessage(bool @bool)
    {
        for (int i = 0; i < this.sendSimulateMessage.Count; i++)
        {
            Transform item = this.sendSimulateMessage[i];
            if (@bool)
            {
                item.gameObject.SendMessage("OnSimulate", SendMessageOptions.DontRequireReceiver);
            }
            else if (item != null)
            {
                item.gameObject.SendMessage("OnStopSimulate", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    public override void SetBlockType(int type)
    {
        if (this._currentBlockType != type)
        {
            this._currentGhostBlockInfo = (PrefabMaster.GetBlock(type) as GenericBlock).MyBlockInfo;
            if (this._currentGhost != null)
            {
                if (this._currentGhostArrow)
                {
                    if (!this._currentGhostFlipped)
                    {
                        Vector3 vector3 = this._currentGhostArrow.localScale;
                        float single = Mathf.Abs(vector3.x) * -1f;
                        float single1 = single;
                        Vector3 vector31 = this._currentGhostArrow.localScale;
                        Vector3 vector32 = vector31;
                        float single2 = single1;
                        float single3 = single2;
                        vector32.x = single2;
                        Vector3 vector33 = vector32;
                        Vector3 vector34 = vector33;
                        this._currentGhostArrow.localScale = vector33;
                    }
                    else
                    {
                        Vector3 vector35 = this._currentGhostArrow.localScale;
                        float single4 = Mathf.Abs(vector35.x) * 1f;
                        float single5 = single4;
                        Vector3 vector36 = this._currentGhostArrow.localScale;
                        Vector3 vector37 = vector36;
                        float single6 = single5;
                        float single7 = single6;
                        vector37.x = single6;
                        Vector3 vector38 = vector37;
                        Vector3 vector39 = vector38;
                        this._currentGhostArrow.localScale = vector38;
                    }
                }
                this._currentGhost.gameObject.SetActive(false);
            }
            this._currentGhostFlipped = false;
            if (this._currentGhostBlockInfo.ghost == null)
            {
                this._currentGhost = this.ghostUnknown;
                this._currentGhostController = this.ghostUnknownMaterialController;
                this._currentGhostTrigger = null;
            }
            else
            {
                this._currentGhost = this._currentGhostBlockInfo.ghost.transform;
                this._currentGhostController = this._currentGhost.GetComponent<GhostMaterialController>();
                this._currentGhostTrigger = this._currentGhost.GetComponentInChildren<GhostTrigger>();
            }
            this._currentGhostArrow = this._currentGhost.FindChild("DirectionArrow");
            this._currentHammerObj = this._currentGhost.FindChild("HammerPos");
            this._currentBlockType = type;
        }
    }

    public override IEnumerator Simulate()
    {
        return (new AddPiece.$Simulate$2186(this)).GetEnumerator();
    }

    public override IEnumerator SimulateOneFrame(float simulationSpeed)
    {
        return (new AddPiece.$SimulateOneFrame$2200(simulationSpeed)).GetEnumerator();
    }

    protected override void Start()
    {
        this.SetBlockType(1);
        StatMaster.ChangeSelectedBlock(1);
        int num = 0;
        BlockMenuControl[] blockMenuControlArray = this.blockMenuControl;
        int length = blockMenuControlArray.Length;
        while (num < length)
        {
            blockMenuControlArray[num].UpdateButtons();
            num++;
        }
    }

    protected override void Update()
    {
        float single;
        object obj;
        if (!StatMaster.inMenu && InputManager.ToggleSimulationKey() && !StatMaster.stopHotkeys)
        {
            this.StartCoroutine_Auto(this.Simulate());
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (!Input.GetKeyDown(KeyCode.Return))
            {
                Type type = typeof(Input);
                object[] objArray = new object[1];
                int num = 13;
                if (num != 0)
                {
                    obj = this.enterTimer > 0.2f;
                }
                else
                {
                    obj = (KeyCode)num;
                }
                objArray[0] = obj;
                if (!RuntimeServices.ToBool(UnityRuntimeServices.Invoke(type, "GetKey", objArray, typeof(MonoBehaviour))))
                {
                    goto Label1;
                }
            }
            this.StartCoroutine_Auto(this.SimulateOneFrame((!Input.GetKey(KeyCode.LeftShift) ? 0.1f : 0.01f)));
            this.enterTimer = this.enterTimer + Time.deltaTime;
            goto Label0;
        }
        Label1:
        this.enterTimer = (float)0;
        Label0:
        single = (!StatMaster.isSimulating || WinCondition.hasWon ? (float)0 : AddPiece.timeTaken + Time.deltaTime);
        AddPiece.timeTaken = single;
        if (!StatMaster.isSimulating || !AddPiece.canSimulate)
        {
            Vector2 vector2 = Input.mousePosition;
            this.rayHud = this.hudCam.ScreenPointToRay(new Vector3(vector2.x, vector2.y, (float)0));
            this.hudOccluding = Physics.Raycast(this.rayHud, out this.hudHit, (float)1000, this.layerMaskyHud);
            StatMaster.hudOccluding = this.hudOccluding;
            if (AddPiece.disableBlockPlacement)
            {
                this.BlockDeselect();
                BlockMapper.Close();
            }
            else if (AddPiece.keyMapMode)
            {
                if (!this.hudOccluding && this.HoveredBlock != null && Input.GetButtonDown("Fire1"))
                {
                    this.BlockSelect(this.HoveredBlock);
                    BlockMapper.Open(this.HoveredBlock);
                }
                if (!BlockMapper.CurrentInstance)
                {
                    this.BlockDeselect();
                }
            }
            else if (!AddPiece.selectSymmetryPivot)
            {
                this.BlockDeselect();
                BlockMapper.Close();
            }
            else
            {
                this.BlockDeselect();
                BlockMapper.Close();
            }
            if (!StatMaster.inMenu && !LevelEditController.levelEditActive && !StatMaster.Mode.translateMode)
            {
                bool flag = false;
                bool flag1 = false;
                if (!StatMaster.stopHotkeys && !this.hudOccluding)
                {
                    this.deleteDownTime = (InputManager.DeleteKey() || InputManager.DeleteKeyHeld() ? this.deleteDownTime + Time.deltaTime : (float)0);
                    if (this.deleteDownTime > 0.5f)
                    {
                        if (InputManager.DeleteKeyHeld())
                        {
                            flag1 = true;
                        }
                    }
                    else if (InputManager.DeleteKey())
                    {
                        flag = true;
                    }
                }
                this.canAdd = false;
                this.ray = this.mainCam.ScreenPointToRay(new Vector3(vector2.x, vector2.y, (float)0));
                if (StatMaster.isSimulating || this.hudOccluding)
                {
                    this.mouseHasHit = false;
                }
                else
                {
                    this.mouseHasHit = Physics.Raycast(this.ray, out this.mouseHit, (float)300, this.layerMasky);
                    if (this.checkPinBlocks && (AddPiece.eraseMode || AddPiece.keyMapMode || AddPiece.selectSymmetryPivot || AddPiece.disableBlockPlacement || flag || flag1))
                    {
                        RaycastHit raycastHit = new RaycastHit();
                        if (Physics.Raycast(this.ray, out raycastHit, (float)300, this.pinMask) && raycastHit.rigidbody.gameObject.GetComponent<BlockBehaviour>() != null)
                        {
                            this.mouseHit = raycastHit;
                            this.mouseHasHit = true;
                        }
                    }
                }
                if (!this.mouseHasHit || this.hudOccluding)
                {
                    this.BlockHoverOut();
                }
                else
                {
                    int num1 = this.mouseHit.collider.gameObject.layer;
                    if (this.BlockType == 5000)
                    {
                        this.canAdd = num1 == 14;
                    }
                    else if (num1 == 12 || num1 == 14)
                    {
                        this.canAdd = true;
                    }
                    BlockBehaviour componentInParent = this.mouseHit.collider.gameObject.GetComponentInParent<BlockBehaviour>();
                    if (componentInParent == null)
                    {
                        this.BlockHoverOut();
                    }
                    else
                    {
                        this.BlockHoverOver(componentInParent);
                    }
                }
                if (!this.hudOccluding && !AddPiece.keyMapMode && !AddPiece.selectSymmetryPivot && !AddPiece.disableBlockPlacement)
                {
                    if (!AddPiece.eraseMode && Input.GetButtonDown("Fire1"))
                    {
                        this.AddBlockType(this.activeGhost);
                        this.symmetryController.AddSymBlocks();
                    }
                    else if (AddPiece.eraseMode && Input.GetButtonUp("Fire1"))
                    {
                        this.StartCoroutine_Auto(this.RemoveBlock());
                    }
                }
                if (!StatMaster.stopHotkeys)
                {
                    if (flag1)
                    {
                        this.StartCoroutine_Auto(this.RemoveBlock());
                    }
                    else if (flag)
                    {
                        this.StartCoroutine_Auto(this.RemoveBlock());
                    }
                    if (InputManager.ReverseKey() && this.mouseHit.collider)
                    {
                        Transform transforms = this.mouseHit.collider.transform.parent;
                        if (transforms)
                        {
                            Machine.Active().UndoSystem.FlipSnapshot(transforms.gameObject);
                        }
                    }
                    if (InputManager.RedoKeys())
                    {
                        Machine.Active().UndoSystem.Redo();
                    }
                    if (InputManager.UndoKeys())
                    {
                        Machine.Active().UndoSystem.Undo();
                    }
                    if (InputManager.RotateKey())
                    {
                        this.rotationAmount = this.rotationAmount + (float)90;
                    }
                }
                this.UpdateGhost();
            }
        }
        else
        {
            Machine machine = Machine.Active();
            if (machine)
            {
                this.middleOfObject.position = machine.CalculateMiddle();
            }
        }
    }

    public override void UpdateBlockAndMassText(float mass, int count)
    {
        if (this.blockCountTextMesh != null)
        {
            this.blockCountTextMesh.text = count.ToString();
        }
        if (this.massTextMesh != null)
        {
            this.massTextMesh.text = (mass / 8f).ToString("f1");
        }
    }

    public override void UpdateCOM()
    {
        this.comCode.GetCOM(this.machineTracker.BuildingMachine);
    }

    private void UpdateGhost()
    {
        bool flag;
        AddPiece.ghostEnabled = false;
        if (!this.hudOccluding && !AddPiece.eraseMode && !AddPiece.keyMapMode && !AddPiece.selectSymmetryPivot && !AddPiece.disableBlockPlacement && !StatMaster.Mode.translateMode)
        {
            this.activeGhost = null;
            if (this.canAdd && this._currentGhost != null)
            {
                AddPiece.ghostEnabled = true;
                this.activeGhost = this._currentGhost;
                GameObject gameObject = this._currentGhost.gameObject;
                if (!gameObject.activeInHierarchy)
                {
                    gameObject.SetActive(true);
                    this.symmetryController.UpdateSymmetryTransforms();
                }
                Vector3 vector3 = this._currentGhost.position;
                Quaternion quaternion = this._currentGhost.rotation;
                Vector3 vector31 = this.mouseHit.collider.transform.position;
                if (this._currentGhostBlockInfo.placeMode != PlaceMode.Center)
                {
                    AddPiece.mouseHitNormal = this.mouseHit.normal;
                    AddPiece.mouseHitPos = vector31 + (AddPiece.mouseHitNormal / 2f);
                }
                else
                {
                    AddPiece.mouseHitPos = vector31;
                }
                Transform transforms = null;
                transforms = (!this.mouseHit.rigidbody ? this.mouseHit.transform : this.mouseHit.rigidbody.transform);
                Vector3 localDirClosestTo = transforms.forward;
                localDirClosestTo = AddPiece.GetLocalDirClosestTo(transforms, Machine.Active().BuildingMachine.up);
                if (Vector3.Angle(AddPiece.mouseHitNormal, localDirClosestTo) <= 45f || Vector3.Angle(AddPiece.mouseHitNormal, -localDirClosestTo) <= 45f)
                {
                    localDirClosestTo = AddPiece.GetLocalDirClosestTo(transforms, Machine.Active().BuildingMachine.forward);
                    if (Vector3.Angle(AddPiece.mouseHitNormal, Machine.Active().BuildingMachine.up) <= 45f)
                    {
                        localDirClosestTo = localDirClosestTo * -1f;
                    }
                }
                PlaceMode placeMode = this._currentGhostBlockInfo.placeMode;
                if (placeMode == PlaceMode.Normal)
                {
                    AddPiece.blockPlacedRotation = Quaternion.LookRotation(AddPiece.mouseHitNormal, localDirClosestTo) * Quaternion.Euler(new Vector3((float)0, (float)0, this.rotationAmount));
                }
                else if (placeMode == PlaceMode.Center)
                {
                    float single = (float)0;
                    Vector3 vector32 = this.mouseHit.transform.eulerAngles;
                    AddPiece.blockPlacedRotation = Quaternion.Euler(single, vector32.y + this.rotationAmount, (float)0);
                }
                else if (placeMode == PlaceMode.Camera)
                {
                    if (Vector3.Angle(AddPiece.mouseHitNormal, Machine.Active().BuildingMachine.up) <= 45f || Vector3.Angle(-AddPiece.mouseHitNormal, Machine.Active().BuildingMachine.up) <= 45f)
                    {
                        AddPiece.blockPlacedRotation = Quaternion.LookRotation(AddPiece.mouseHitNormal, localDirClosestTo) * Quaternion.Euler(new Vector3((float)0, (float)0, this.rotationAmount));
                    }
                    else
                    {
                        AddPiece.blockPlacedRotation = Quaternion.LookRotation(AddPiece.mouseHitNormal, localDirClosestTo);
                    }
                }
                else if (placeMode != PlaceMode.Rocket)
                {
                    AddPiece.blockPlacedRotation = Quaternion.LookRotation(Machine.Active().BuildingMachine.forward, Machine.Active().BuildingMachine.up) * Quaternion.Euler(new Vector3((float)0, this.rotationAmount, (float)0));
                }
                else if (Vector3.Angle(AddPiece.mouseHitNormal, Machine.Active().BuildingMachine.up) <= 45f || Vector3.Angle(-AddPiece.mouseHitNormal, Machine.Active().BuildingMachine.up) <= 45f)
                {
                    AddPiece.blockPlacedRotation = Quaternion.LookRotation(AddPiece.mouseHitNormal, localDirClosestTo) * Quaternion.Euler(new Vector3((float)0, (float)0, 180f + this.rotationAmount));
                }
                else
                {
                    AddPiece.blockPlacedRotation = Quaternion.LookRotation(AddPiece.mouseHitNormal, localDirClosestTo) * Quaternion.Euler(new Vector3((float)0, (float)0, this.rotationAmount));
                }
                bool flag1 = vector3 != AddPiece.mouseHitPos;
                if (!flag1)
                {
                    flag1 = quaternion != AddPiece.blockPlacedRotation;
                }
                if (flag1)
                {
                    if (this._currentGhostTrigger)
                    {
                        this._currentGhostTrigger.touchingCount = 0;
                    }
                    this._currentGhost.parent.rotation = AddPiece.blockPlacedRotation * AddPiece.PlacementOffset.rotation;
                    this._currentGhost.parent.position = AddPiece.mouseHitPos;
                    this._currentGhost.localPosition = AddPiece.PlacementOffset.position;
                    AddPiece.hammerPos = this._currentHammerObj.position;
                    AddPiece.hammerFwd = this._currentHammerObj.forward;
                    if (this._currentGhostArrow && this.BlockType != 13 && this.BlockType != 28 && this.CompareVectors(this._currentGhost.forward, -Vector3.right, (float)45) != this._currentGhostFlipped)
                    {
                        if (!this._currentGhostFlipped)
                        {
                            Vector3 vector33 = this._currentGhostArrow.localScale;
                            float single1 = Mathf.Abs(vector33.x) * -1f;
                            float single2 = single1;
                            Vector3 vector34 = this._currentGhostArrow.localScale;
                            Vector3 vector35 = vector34;
                            float single3 = single2;
                            float single4 = single3;
                            vector35.x = single3;
                            Vector3 vector36 = vector35;
                            Vector3 vector37 = vector36;
                            this._currentGhostArrow.localScale = vector36;
                        }
                        else
                        {
                            Vector3 vector38 = this._currentGhostArrow.localScale;
                            float single5 = Mathf.Abs(vector38.x) * 1f;
                            float single6 = single5;
                            Vector3 vector39 = this._currentGhostArrow.localScale;
                            Vector3 vector310 = vector39;
                            float single7 = single6;
                            float single8 = single7;
                            vector310.x = single7;
                            Vector3 vector311 = vector310;
                            Vector3 vector312 = vector311;
                            this._currentGhostArrow.localScale = vector311;
                        }
                        this._currentGhostFlipped = !this._currentGhostFlipped;
                    }
                    this.symmetryController.UpdateSymmetryTransforms();
                }
                if (gameObject.GetComponent<Rigidbody>() != null)
                {
                    flag = GhostTrigger.isTouching;
                    if (!flag)
                    {
                        flag = this.outOfBounds;
                    }
                }
                else
                {
                    flag = this.outOfBounds;
                }
                AddPiece.individualOutOfBounds = flag;
            }
            else if (this._currentGhost != null)
            {
                GameObject gameObject1 = this._currentGhost.gameObject;
                if (gameObject1.activeInHierarchy)
                {
                    gameObject1.transform.localRotation = Quaternion.identity;
                    gameObject1.SetActive(false);
                    this.symmetryController.DisableSymGameObjects();
                }
            }
        }
        else if (this.activeGhost)
        {
            this.activeGhost.gameObject.SetActive(false);
            if (this.symmetryController)
            {
                this.symmetryController.DisableSymGameObjects();
            }
            this.activeGhost = null;
        }
    }

    public override void UpdateMiddleOfObject()
    {
        this.UpdateMiddleOfObject(false);
    }

    public override void UpdateMiddleOfObject(bool fullCalculationForced)
    {
        this.UpdateCOM();
        if (fullCalculationForced && SingleInstanceFindOnly<MouseOrbit>.Instance.target == SingleInstanceFindOnly<MouseOrbit>.Instance.machineTarget)
        {
            SingleInstanceFindOnly<MouseOrbit>.Instance.wasdPosOffset = Vector3.zero;
        }
        this.middleOfObject.position = Machine.Active().CalculateMiddle(fullCalculationForced);
        this.lastMiddleOfObject = this.middleOfObject.position;
    }

    [CompilerGenerated]
    [Serializable]
    internal sealed class $RemoveBlock$2203 : GenericGenerator<object>
    {
        internal AddPiece $self_$2206;

        public $RemoveBlock$2203(AddPiece self_)
        {
            this.$self_$2206 = self_;
        }

public override IEnumerator<object> GetEnumerator()
{
    return new AddPiece.$RemoveBlock$2203.$(this.$self_$2206);
}

[CompilerGenerated]
[Serializable]
internal sealed class $ : GenericGeneratorEnumerator<object>, IEnumerator
        {
            internal Machine $machine$2204;

            internal AddPiece $self_$2205;

            public $(AddPiece self_)
            {
                this.$self_$2205 = self_;
            }

public override bool MoveNext()
{
    bool flag;
    switch (this._state)
    {
        case 1:
            {
                flag = false;
                break;
            }
        case 2:
            {
                this.$self_$2205.UpdateMiddleOfObject();
                this.YieldDefault(1);
                goto case 1;
            }
        default:
            {
                if (!this.$self_$2205._hoveredBlock || this.$self_$2205._hoveredBlock.BlockID == 0)
                        {
                    goto case 1;
                }
                        else
                        {
                    this.$machine$2204 = this.$self_$2205._hoveredBlock.GetComponentInParent<Machine>();
                    if (!this.$machine$2204)
                            {
                        this.$machine$2204 = Machine.Active();
                    }
                    if (this.$machine$2204)
                            {
                        this.$machine$2204.UnregisterBlock(this.$self_$2205._hoveredBlock);
                    }
                    Object.Destroy(this.$self_$2205._hoveredBlock.gameObject);
                    this.$self_$2205._hoveredBlock = null;
                    this.$self_$2205.deleteSound.Stop();
                    this.$self_$2205.deleteSound.Play();
                    this.$self_$2205.boundVisCode.Check();
                    if (this.$machine$2204)
                            {
                        this.$machine$2204.UndoSystem.Snapshot();
                    }
                    flag = this.YieldDefault(2);
                    break;
                }
            }
    }
    return flag;
}
        }
    }

    [CompilerGenerated]
[Serializable]
internal sealed class $Simulate$2186 : GenericGenerator<YieldInstruction>
    {
        internal AddPiece $self_$2199;

        public $Simulate$2186(AddPiece self_)
        {
            this.$self_$2199 = self_;
        }

public override IEnumerator<YieldInstruction> GetEnumerator()
{
    return new AddPiece.$Simulate$2186.$(this.$self_$2199);
}

[CompilerGenerated]
[Serializable]
internal sealed class $ : GenericGeneratorEnumerator<YieldInstruction>, IEnumerator
        {
            internal TimeSlider $timeSlider$2187;

            internal float $ts$2188;

            internal bool $setBarEarly$2189;

            internal Vector3 $grav$2190;

            internal Transform $physInstance$2191;

            internal Rigidbody[] $rs$2192;

            internal Rigidbody $r$2193;

            internal MouseOrbit $mouseOrbit$2194;

            internal int $$539$2195;

            internal Rigidbody[] $$540$2196;

            internal int $$541$2197;

            internal AddPiece $self_$2198;

            public $(AddPiece self_)
            {
                this.$self_$2198 = self_;
            }

public override bool MoveNext()
{
    bool flag;
    bool flag1;
    switch (this._state)
    {
        case 1:
            {
                flag = false;
                return flag;
            }
        case 2:
            {
                AddPiece.ghostEnabled = false;
                if (StatMaster.useSmartInterpolation && this.$ts$2188 >= 0.6f)
                        {
                    this.$rs$2192 = ReferenceMaster.physicsGoalInstance.GetComponentsInChildren<Rigidbody>();
                    this.$$539$2195 = 0;
                    this.$$540$2196 = this.$rs$2192;
                    this.$$541$2197 = this.$$540$2196.Length;
                            while (this.$$539$2195 < this.$$541$2197)
                            {
                                if (!this.$$540$2196[this.$$539$2195].isKinematic)
                                {
                                    this.$$540$2196[this.$$539$2195].interpolation = RigidbodyInterpolation.None;
                                }
                                this.$$539$2195 = this.$$539$2195 + 1;
                            }
                        }
                        flag = this.YieldDefault(3);
                        break;
                    }
                    case 3:
                    {
                        this.$timeSlider$2187.startingSimulation = false;
                        this.$timeSlider$2187.wasSimulating = false;
                        if (!this.$setBarEarly$2189)
                        {
                            this.$self_$2198.barPosCode.Set();
                        }
                        Time.timeScale = 1f;
                        flag = this.Yield(4, new WaitForFixedUpdate());
                        break;
                    }
                    case 4:
                    {
                        flag = this.Yield(5, new WaitForFixedUpdate());
                        break;
                    }
                    case 5:
                    {
                        Physics.gravity = this.$grav$2190;
                        AddPiece.canSimulate = true;
                        flag1 = this.YieldDefault(1);
                        flag = false;
                        return flag;
                    }
                    default:
                    {
                        if (this.$self_$2198.outOfBounds)
                        {
                            this.$self_$2198.OutOfBoundsWarningCode.OutOfBounds();
                            flag = false;
                            return flag;
                        }
                        else if (AddPiece.canSimulate)
                        {
                            StatMaster.isSimulating = !StatMaster.isSimulating;
                            StatMaster.wasSimulating = StatMaster.isSimulating;
                            if ((StatMaster.GodTools.PyroMode || StatMaster.GodTools.DragMode || StatMaster.GodTools.UnbreakableMode || StatMaster.GodTools.InfiniteAmmoMode || StatMaster.GodTools.GravityDisabled) && this.$self_$2198.GodToolsWarningCode != null && StatMaster.isSimulating)
                            {
                                this.$self_$2198.GodToolsWarningCode.CheatsEnabled();
                            }
                            this.$self_$2198.SendSimulateMessage(StatMaster.isSimulating);
                            this.$self_$2198.boundVisCode.FadeVis();
                            BlockMapper.Close();
                            this.$self_$2198.symmetryController.ClearSymGameObjects();
                            if (!StatMaster.isSimulating)
                            {
                                AddPiece.canSimulate = false;
                                this.$self_$2198.playButton.Stop();
                                Machine.Active().EndSimulation();
                                Object.Destroy(ReferenceMaster.physicsGoalInstance.gameObject);
                                this.$self_$2198.physicsGoalObject.gameObject.SetActive(true);
                                this.$self_$2198.barPosCode.Set();
                                this.$mouseOrbit$2194 = SingleInstanceFindOnly<MouseOrbit>.Instance;
                                this.$self_$2198.middleOfObject.position = this.$self_$2198.lastMiddleOfObject;
                                AddPiece.canSimulate = true;
                                flag1 = this.YieldDefault(1);
                                flag = false;
                                return flag;
                            }
                            else
                            {
                                this.$self_$2198.playButton.Play();
                                AddPiece.canSimulate = false;
                                this.$timeSlider$2187 = SingleInstance<TimeSlider>.Instance;
                                this.$timeSlider$2187.startingSimulation = true;
                                this.$ts$2188 = this.$timeSlider$2187.delegateTimeScale;
                                this.$setBarEarly$2189 = this.$timeSlider$2187.delegateTimeScale == (float)0;
                                if (this.$setBarEarly$2189)
                                {
                                    this.$self_$2198.barPosCode.Set();
                                }
                                Time.timeScale = (float)0;
                                this.$grav$2190 = Physics.gravity;
                                Physics.gravity = Vector3.zero;
                                this.$self_$2198.physicsGoalObject.gameObject.SetActive(false);
                                this.$physInstance$2191 = ((GameObject)Object.Instantiate(this.$self_$2198.physicsGoalObject.gameObject, this.$self_$2198.physicsGoalObject.position, this.$self_$2198.physicsGoalObject.rotation)).transform;
                                this.$physInstance$2191.gameObject.SetActive(true);
                                this.$physInstance$2191.name = "PHYSICS GOAL";
                                ReferenceMaster.physicsGoalInstance = this.$physInstance$2191;
                                flag = this.Yield(2, this.$self_$2198.StartCoroutine(Machine.Active().StartSimulation()));
                                break;
                            }
                        }
                        else
                        {
                            flag = false;
                            return flag;
                        }
                    }
                }
                return flag;
            }
        }
    }

    [CompilerGenerated]
    [Serializable]
    internal sealed class $SimulateOneFrame$2200 : GenericGenerator<object>
    {
        internal float $simulationSpeed$2202;

        public $SimulateOneFrame$2200(float simulationSpeed)
        {
            this.$simulationSpeed$2202 = simulationSpeed;
        }

        public override IEnumerator<object> GetEnumerator()
        {
            return new AddPiece.$SimulateOneFrame$2200.$(this.$simulationSpeed$2202);
        }

        [CompilerGenerated]
        [Serializable]
        internal sealed class $ : GenericGeneratorEnumerator<object>, IEnumerator
        {
            internal float $simulationSpeed$2201;

            public $(float simulationSpeed)
            {
                this.$simulationSpeed$2201 = simulationSpeed;
            }

            public override bool MoveNext()
            {
                bool flag;
                switch (this._state)
                {
                    case 1:
                    {
                        flag = false;
                        break;
                    }
                    case 2:
                    {
                        Time.timeScale = (float)0;
                        this.YieldDefault(1);
                        goto case 1;
                    }
                    default:
                    {
                        Time.timeScale = this.$simulationSpeed$2201;
                        flag = this.YieldDefault(2);
                        break;
                    }
                }
                return flag;
            }
        }
    }

    [Serializable]
    public class DirAnglePair
    {
        public Vector3 dir;

        public float angle;

        public DirAnglePair()
        {
        }
    }

    public static class PlacementOffset
    {
        [NonSerialized]
        public static Vector3 position;

        [NonSerialized]
        public static Quaternion rotation;

        static PlacementOffset()
        {
            AddPiece.PlacementOffset.position = Vector3.zero;
            AddPiece.PlacementOffset.rotation = Quaternion.identity;
        }

        public static void Reset()
        {
            AddPiece.PlacementOffset.position = Vector3.zero;
            AddPiece.PlacementOffset.rotation = Quaternion.identity;
        }

        public static void Set(Vector3 position, Quaternion rotation)
        {
            AddPiece.PlacementOffset.position = position;
            AddPiece.PlacementOffset.rotation = rotation;
        }
    }
}
