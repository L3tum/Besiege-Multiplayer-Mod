using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Boo.Lang.Runtime;
using UnityEngine;
using UnityScript.Lang;
using Object = System.Object;
using Random = System.Random;

namespace BesiegeMP
{
    [Serializable]
    class MachineMP : Machine
    {
        private string _name;

        private Dictionary<string, XData> additionalData;

        private bool _isFirstMiddle;

        public AddPieceMP addPiece;

        private Vector3 _machineMiddle;

        private Vector3 _tempMiddle;

        private Vector3 _lastDelta;

        private bool _hasTempMiddle;

        private int _middleIncrement;

        private Vector3 smoothFollow;

        private Transform machine;

        private Transform simulationClone;

        private UndoSystem undoSystem;

        private Bounds machineBounds;

        private float machineMass;

        private List<BlockBehaviour> simulationBlocks;

        private Dictionary<int, BlockBehaviour> ignoreBlocks;

        private Dictionary<int, List<BlockBehaviour>> connectionList;

        public bool ignoreDisconnectedBlocks;

        public bool useEndPointWeights;

        private bool notifyWaitWrite;

        public float notifyDelay;

        private Queue<BlockBehaviour> notifyList;

        private Dictionary<int, BlockBehaviour> notifyDictionary;

        private Queue<BlockBehaviour> processBlockList;

        private Dictionary<int, BlockBehaviour> tempIgnoreBlocks;

        private HashSet<int> activeBlocksSet;

        private LinkedList<BlockBehaviour> machineEndPoints;

        private Dictionary<BlockBehaviour, int> endPointWeights;

        private Vector3 cameraAverage;

        private Vector3 cameraOffset;

        private bool cameraUpdate;

        private Vector3 oldPosition;

        private Vector3 lastMachinePosition;

        private bool enableAdaptiveCam;

        public override Dictionary<string, XData> AdditionalData
        {
            get { return new Dictionary<string, XData>(this.additionalData); }
        }

        public override List<BlockBehaviour> Blocks
        {
            get { return new List<BlockBehaviour>((!this.simulationClone ? ReferenceMaster.BuildingBlocks : this.simulationBlocks)); }
        }

        public override Bounds Bounds
        {
            get
            {
                this.UpdateBounds();
                return this.machineBounds;
            }
        }

        public override List<BlockBehaviour> BuildingBlocks
        {
            get { return ReferenceMaster.BuildingBlocks; }
        }

        public override Transform BuildingMachine
        {
            get { return this.machine; }
        }

        public override float Mass
        {
            get
            {
                this.UpdateMass();
                return this.machineMass;
            }
        }

        public override Vector3 MiddlePosition
        {
            get { return this._machineMiddle; }
        }

        public override string Name
        {
            get { return this._name; }
            set
            {
                this.transform.name = "Machine - " + value;
                this._name = value;
            }
        }

        public override Vector3 Position
        {
            get { return this.machine.transform.position; }
            set { this.machine.transform.position = value; }
        }

        public override Quaternion Rotation
        {
            get { return this.machine.transform.rotation; }
            set { this.machine.transform.rotation = value; }
        }

        public override Transform SimulationMachine
        {
            get { return this.simulationClone; }
        }

        public override Vector3 SmoothFollowPosition
        {
            get { return this.smoothFollow; }
        }

        public override UndoSystem UndoSystem
        {
            get { return this.undoSystem; }
        }

        internal string LoadMachineInfo(XData x)
        {
            return x.Key;
        }

        public MachineMP()
        {
            this._name = "Machine";
            this.additionalData = new Dictionary<string, XData>();
            this._machineMiddle = Vector3.zero;
            this._tempMiddle = Vector3.zero;
            this._lastDelta = Vector3.zero;
            this._middleIncrement = 1;
            this.smoothFollow = Vector3.zero;
            this.simulationBlocks = new List<BlockBehaviour>();
            this.ignoreBlocks = new Dictionary<int, BlockBehaviour>();
            this.connectionList = new Dictionary<int, List<BlockBehaviour>>();
            this.ignoreDisconnectedBlocks = true;
            this.useEndPointWeights = true;
            this.notifyDelay = 0.2f;
            this.notifyList = new Queue<BlockBehaviour>();
            this.notifyDictionary = new Dictionary<int, BlockBehaviour>();
            this.processBlockList = new Queue<BlockBehaviour>();
            this.tempIgnoreBlocks = new Dictionary<int, BlockBehaviour>();
            this.activeBlocksSet = new HashSet<int>();
            this.machineEndPoints = new LinkedList<BlockBehaviour>();
            this.endPointWeights = new Dictionary<BlockBehaviour, int>();
            this.cameraAverage = Vector3.zero;
            this.cameraUpdate = true;
        }

        public static Machine Active()
        {
            return SingleInstance<MachineObjectTracker>.Instance.ActiveMachine;
        }

        public override BlockBehaviour AddBlock(BlockInfo blockInfo)
        {
            return this.AddBlock(blockInfo, true);
        }

        private BlockBehaviour AddBlock(BlockInfo blockInfo, bool tryAgain)
        {
            BlockBehaviour blockBehaviour;
            BlockBehaviour block = PrefabMaster.GetBlock(blockInfo.ID);
            if (blockInfo.ID != 8)
            {
                if (block == null)
                {
                    if (!tryAgain)
                    {
                        Debug.LogWarning(("There is no block with ID #" + blockInfo.ID) + ".");
                    }
                    else
                    {
                        Debug.LogWarning(("There is no block with ID #" + blockInfo.ID) + ", trying again.");
                        this.StartCoroutine(this.WaitAndTryAddBlockAgain(blockInfo, 0.2f));
                    }
                    blockBehaviour = null;
                    return blockBehaviour;
                }
                if (blockInfo.ID == 57 || blockInfo.ID == 58)
                {
                    SingleInstance<AddPiece>.Instance.checkPinBlocks = true;
                }
                BlockBehaviour scale = (BlockBehaviour)Instantiate(block, blockInfo.Position, blockInfo.Rotation);
                scale.name = block.name;
                scale.transform.SetParent(this.machine, false);
                scale.transform.localScale = blockInfo.Scale;
                block = scale.GetComponent<BlockBehaviour>();
                block.Guid = blockInfo.Guid;
                Rigidbody[] componentsInChildren = block.GetComponentsInChildren<Rigidbody>();
                int num = 0;
                Rigidbody[] blockSolverIterationCount = componentsInChildren;
                int length = blockSolverIterationCount.Length;
                while (num < length)
                {
                    blockSolverIterationCount[num].solverIterationCount = StaticSettings.BlockSolverIterationCount;
                    num++;
                }
                XDataHolder xDataHolder = null;
                if (!AddPiece.usingCopiedBlock || AddPiece.copiedBlockData == null)
                {
                    xDataHolder = blockInfo.BlockData.Clone();
                    xDataHolder.WasCreated = true;
                    block.OnLoad(xDataHolder);
                }
                else
                {
                    xDataHolder = AddPiece.copiedBlockData.Clone();
                    xDataHolder.WasCreated = true;
                    block.OnLoad(xDataHolder);
                }
                ReferenceMaster.BuildingBlocks.Add(block);
                block.StoreInitialData();
                SingleInstance<AddPiece>.Instance.UpdateBlockAndMassText(this.Mass, StatMaster.BlockCount);
                blockBehaviour = block;
                return blockBehaviour;
            }
            else
            {
                Debug.Log("Tried loading block id 8, unused block known to cause issues, and interfere with modding, refrained from loading block.");
            }
            blockBehaviour = null;
            return blockBehaviour;
        }

        public override BlockBehaviour AddBlock(Vector3 position, Quaternion rotation, int id)
        {
            BlockInfo blockInfo = new BlockInfo()
            {
                ID = id,
                Position = position,
                Rotation = rotation,
                Scale = PrefabMaster.GetDefaultScale(id),
                BlockData = new XDataHolder()
            };
            return this.AddBlock(blockInfo);
        }

        public override BlockBehaviour AddBlockGlobal(Vector3 position, Quaternion rotation, int id)
        {
            BlockInfo blockInfo = new BlockInfo()
            {
                ID = id,
                Position = this.BuildingMachine.InverseTransformPoint(position),
                Rotation = Quaternion.Inverse(this.BuildingMachine.rotation)*rotation,
                Scale = PrefabMaster.GetDefaultScale(id),
                BlockData = new XDataHolder()
            };
            BlockBehaviour blockBehaviour = this.AddBlock(blockInfo);
            blockBehaviour.VisualController.PlaceFromPrefab();
            return blockBehaviour;
        }

        public override void AddMachineData(XData data)
        {
            this.additionalData.Add(data.Key, data);
        }

        public override void Awake()
        {
            this.transform.position = Vector3.zero;
            this.machine = (new GameObject("Building Machine")).transform;
            this.machine.SetParent(this.transform);
            this.machine.position = Vector3.up*(float) 6;
            this.undoSystem = this.gameObject.AddComponent<UndoSystem>();
            this.undoSystem.Machine = this;
            ReferenceMaster.UndoSystemGO = this.gameObject;
            SingleInstance<MachineObjectTracker>.Instance.SetActiveMachine(this);
            this.lastMachinePosition = Vector3.up*6f;
        }

        private void CalculateEndWeights()
        {
            this.endPointWeights.Clear();
            if (this.machineEndPoints.Count != 0)
            {
                bool flag = new bool();
                for (int i = 0; i < this.simulationBlocks.Count; i++)
                {
                    BlockBehaviour item = this.simulationBlocks[i];
                    Transform transforms = item.transform;
                    float single = new float();
                    BlockBehaviour blockBehaviour = null;
                    flag = true;
                    IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.machineEndPoints);
                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        if (!(current is BlockBehaviour))
                        {
                            current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                        }
                        BlockBehaviour blockBehaviour1 = (BlockBehaviour) current;
                        Vector3 vector3 = blockBehaviour1.transform.position - transforms.position;
                        UnityRuntimeServices.Update(enumerator, blockBehaviour1);
                        float single1 = vector3.sqrMagnitude;
                        if (flag || single1 < single)
                        {
                            blockBehaviour = blockBehaviour1;
                            UnityRuntimeServices.Update(enumerator, blockBehaviour1);
                            single = single1;
                            flag = false;
                        }
                        if (i != 0)
                        {
                            continue;
                        }
                        this.endPointWeights.Add(blockBehaviour1, 0);
                        UnityRuntimeServices.Update(enumerator, blockBehaviour1);
                    }
                    this.endPointWeights[blockBehaviour] = this.endPointWeights[blockBehaviour] + this.GetBlockWeight(item.GetBlockID());
                }
            }
        }

        public override Vector3 CalculateMiddle()
        {
            Vector3 vector3;
            List<BlockBehaviour> blockBehaviours = (!this.simulationClone ? ReferenceMaster.BuildingBlocks : this.simulationBlocks);
            Vector3 vector31 = new Vector3();
            int num = 0;
            this._middleIncrement = (this._isFirstMiddle || !AddPiece.isSimulating ? 1 : 2);
            int num1 = (!this._hasTempMiddle ? 0 : 1);
            Vector3 vector32 = new Vector3();
            BlockBehaviour item = null;
            int count = blockBehaviours.Count;
            int num2 = 0;
            for (int i = num1; i < count; i = i + this._middleIncrement)
            {
                item = blockBehaviours[i];
                if (item)
                {
                    if (!(item is BraceCode) && !(item is SpringCode))
                    {
                        vector32 = item.transform.position;
                        vector31 = (i != 0 ? vector31 + vector32 : vector32);
                        if (item.GetComponent<MachineTrackerMyId>() && item.GetComponent<MachineTrackerMyId>().myId == 0)
                        {
                            num2 = i;
                        }
                        num++;
                    }
                }
            }
            if (num != 0)
            {
                Vector3 vector33 = vector31/(float) num;
                if (this._isFirstMiddle || !AddPiece.isSimulating || num == count)
                {
                    this._machineMiddle = vector33;
                }
                else
                {
                    if (this._hasTempMiddle)
                    {
                        Vector3 vector34 = (vector33 + this._tempMiddle)/(float) 2;
                        this._lastDelta = vector34 - this._machineMiddle;
                        this._machineMiddle = vector34;
                    }
                    else
                    {
                        this._tempMiddle = vector33;
                        this._machineMiddle = this._machineMiddle + (this._lastDelta/(float) 2);
                    }
                    this._hasTempMiddle = !this._hasTempMiddle;
                }
                this._isFirstMiddle = false;
                vector3 = this._machineMiddle;
            }
            else
            {
                if (num2 < blockBehaviours.Count && blockBehaviours[num2] != null)
                {
                    this._machineMiddle = blockBehaviours[num2].transform.position;
                }
                else if (!blockBehaviours.Any<BlockBehaviour>() || !(blockBehaviours[0] != null))
                {
                    this._machineMiddle = new Vector3((float) 0, 6f, (float) 0);
                }
                else
                {
                    this._machineMiddle = blockBehaviours[0].transform.position;
                }
                vector3 = this._machineMiddle;
            }
            return vector3;
        }

        public override MachineInfo CreateMachineInfo()
        {
            MachineInfo machineInfo = new MachineInfo()
            {
                Name = this.Name,
                Position = this.BuildingMachine.position,
                Rotation = this.BuildingMachine.rotation
            };
            List<BlockInfo> blockInfos = new List<BlockInfo>();
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(new List<BlockBehaviour>(ReferenceMaster.BuildingBlocks));
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                if (!(current is BlockBehaviour))
                {
                    current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                }
                BlockBehaviour blockBehaviour = (BlockBehaviour) current;
                blockInfos.Add(BlockInfo.FromBlockBehaviour(blockBehaviour));
                UnityRuntimeServices.Update(enumerator, blockBehaviour);
            }
            machineInfo.Blocks = blockInfos;
            XDataHolder xDataHolder = new XDataHolder();
            this.SaveMachineData(xDataHolder);
            machineInfo.MachineData = xDataHolder;
            return machineInfo;
        }

        public override void EndSimulation()
        {
            if (this.simulationClone != null)
            {
                MouseOrbit instance = SingleInstanceFindOnly<MouseOrbit>.Instance;
                if (instance.targetIsBlock)
                {
                    int num = 0;
                    while (num < ReferenceMaster.BuildingBlocks.Count && num < this.simulationBlocks.Count)
                    {
                        BlockBehaviour item = this.simulationBlocks[num];
                        BlockBehaviour blockBehaviour = ReferenceMaster.BuildingBlocks[num];
                        if (!(item != null) || !(blockBehaviour != null) || !(instance.target == item.transform))
                        {
                            num++;
                        }
                        else
                        {
                            instance.target = blockBehaviour.transform;
                            break;
                        }
                    }
                }
                DestroyImmediate(this.simulationClone.gameObject);
            }
            this.simulationBlocks.Clear();
            Resources.UnloadUnusedAssets();
            this.machine.gameObject.SetActive(true);
        }

        private void FindDisconnectedBlocks(BlockBehaviour disconnectedBlock, int ignoreBlockID)
        {
            this.processBlockList.Clear();
            this.tempIgnoreBlocks.Clear();
            bool flag = false;
            BlockBehaviour blockBehaviour = null;
            int instanceID = new int();
            if (!this.ignoreBlocks.ContainsKey(disconnectedBlock.GetInstanceID()))
            {
                this.processBlockList.Enqueue(disconnectedBlock);
            }
            while (this.processBlockList.Count > 0)
            {
                blockBehaviour = this.processBlockList.Dequeue();
                instanceID = blockBehaviour.GetInstanceID();
                if (this.connectionList.ContainsKey(instanceID))
                {
                    IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.connectionList[instanceID]);
                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        if (!(current is BlockBehaviour))
                        {
                            current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                        }
                        BlockBehaviour blockBehaviour1 = (BlockBehaviour) current;
                        int num = blockBehaviour1.GetInstanceID();
                        UnityRuntimeServices.Update(enumerator, blockBehaviour1);
                        if (num == ignoreBlockID || this.tempIgnoreBlocks.ContainsKey(num) || this.ignoreBlocks.ContainsKey(num))
                        {
                            continue;
                        }
                        this.processBlockList.Enqueue(blockBehaviour1);
                        UnityRuntimeServices.Update(enumerator, blockBehaviour1);
                    }
                }
                BlockBehaviour blockBehaviour2 = this.FindJointConnection(blockBehaviour, true, ignoreBlockID);
                if (blockBehaviour2)
                {
                    this.processBlockList.Enqueue(blockBehaviour2);
                }
                GenericBlock genericBlock = blockBehaviour as GenericBlock;
                if (!genericBlock || !this.activeBlocksSet.Contains(genericBlock.BlockID))
                {
                    this.tempIgnoreBlocks.Add(instanceID, blockBehaviour);
                }
                else
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                IEnumerator enumerator1 = UnityRuntimeServices.GetEnumerator(this.tempIgnoreBlocks);
                while (enumerator1.MoveNext())
                {
                    KeyValuePair<int, BlockBehaviour> keyValuePair = (KeyValuePair<int, BlockBehaviour>) enumerator1.Current;
                    if (this.ignoreBlocks.ContainsKey(keyValuePair.Key))
                    {
                        continue;
                    }
                    this.ignoreBlocks.Add(keyValuePair.Key, keyValuePair.Value);
                    UnityRuntimeServices.Update(enumerator1, keyValuePair);
                }
            }
        }

        private void FindEndPoints()
        {
            this.processBlockList.Clear();
            this.tempIgnoreBlocks.Clear();
            this.machineEndPoints.Clear();
            BlockBehaviour blockBehaviour = null;
            int instanceID = new int();
            bool flag = false;
            bool flag1 = false;
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.simulationBlocks);
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                if (!(current is BlockBehaviour))
                {
                    current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                }
                blockBehaviour = (BlockBehaviour) current;
                if (!blockBehaviour || this.ignoreBlocks.ContainsKey(blockBehaviour.GetInstanceID()))
                {
                    continue;
                }
                this.processBlockList.Enqueue(blockBehaviour);
                UnityRuntimeServices.Update(enumerator, blockBehaviour);
                break;
            }
            if (blockBehaviour != null)
            {
                while (this.processBlockList.Count > 0)
                {
                    blockBehaviour = this.processBlockList.Dequeue();
                    instanceID = blockBehaviour.GetInstanceID();
                    flag = false;
                    flag1 = false;
                    if (this.connectionList.ContainsKey(instanceID))
                    {
                        IEnumerator enumerator1 = UnityRuntimeServices.GetEnumerator(this.connectionList[instanceID]);
                        while (enumerator1.MoveNext())
                        {
                            object obj = enumerator1.Current;
                            if (!(obj is BlockBehaviour))
                            {
                                obj = RuntimeServices.Coerce(obj, typeof (BlockBehaviour));
                            }
                            BlockBehaviour blockBehaviour1 = (BlockBehaviour) obj;
                            if (blockBehaviour1 != null)
                            {
                                int num = blockBehaviour1.GetInstanceID();
                                UnityRuntimeServices.Update(enumerator1, blockBehaviour1);
                                if (this.tempIgnoreBlocks.ContainsKey(num) || this.ignoreBlocks.ContainsKey(num))
                                {
                                    continue;
                                }
                                this.processBlockList.Enqueue(blockBehaviour1);
                                UnityRuntimeServices.Update(enumerator1, blockBehaviour1);
                                flag = true;
                            }
                        }
                    }
                    GenericBlock genericBlock = blockBehaviour as GenericBlock;
                    if (this.HasJointConnection(blockBehaviour, false, 0) && genericBlock.BlockID != 7)
                    {
                        flag1 = true;
                    }
                    if (flag ^ flag1)
                    {
                        this.machineEndPoints.AddLast(blockBehaviour);
                    }
                    this.tempIgnoreBlocks.Add(instanceID, blockBehaviour);
                }
                if (this.useEndPointWeights)
                {
                    this.CalculateEndWeights();
                }
            }
        }

        private BlockBehaviour FindJointConnection(BlockBehaviour blockToFind, bool checkIgnoreID, int ignoreBlockID)
        {
            GenericBlock genericBlock = blockToFind as GenericBlock;
            BlockBehaviour blockBehaviour = null;
            if (genericBlock.BlockID != 7)
            {
                Joint component = blockToFind.GetComponent<Joint>();
                if (component && component.connectedBody)
                {
                    BlockBehaviour component1 = component.connectedBody.GetComponent<BlockBehaviour>();
                    if (component1)
                    {
                        int instanceID = component1.GetInstanceID();
                        if (!this.tempIgnoreBlocks.ContainsKey(instanceID) && !this.ignoreBlocks.ContainsKey(instanceID) && (!checkIgnoreID || instanceID != ignoreBlockID))
                        {
                            blockBehaviour = component1;
                        }
                    }
                }
            }
            else
            {
                Joint[] components = blockToFind.GetComponents<ConfigurableJoint>();
                if (components.Length == 2)
                {
                    BlockBehaviour blockBehaviour1 = components[1].connectedBody.GetComponent<BlockBehaviour>();
                    int num = blockBehaviour1.GetInstanceID();
                    if (!this.tempIgnoreBlocks.ContainsKey(num) && !this.ignoreBlocks.ContainsKey(num) && (!checkIgnoreID || num != ignoreBlockID))
                    {
                        blockBehaviour = blockBehaviour1;
                    }
                    BlockBehaviour component2 = components[0].connectedBody.GetComponent<BlockBehaviour>();
                    int instanceID1 = component2.GetInstanceID();
                    if (!this.tempIgnoreBlocks.ContainsKey(instanceID1) && !this.ignoreBlocks.ContainsKey(instanceID1) && (!checkIgnoreID || instanceID1 != ignoreBlockID))
                    {
                        blockBehaviour = component2;
                    }
                }
            }
            return blockBehaviour;
        }

        public override BlockBehaviour GetBlock(Transform transform)
        {
            BlockBehaviour blockBehaviour;
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator((!this.simulationClone ? ReferenceMaster.BuildingBlocks : this.simulationBlocks));
            while (true)
            {
                if (enumerator.MoveNext())
                {
                    object current = enumerator.Current;
                    if (!(current is BlockBehaviour))
                    {
                        current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                    }
                    BlockBehaviour blockBehaviour1 = (BlockBehaviour) current;
                    if (blockBehaviour1.transform == transform)
                    {
                        blockBehaviour = blockBehaviour1;
                        break;
                    }
                }
                else
                {
                    blockBehaviour = null;
                    break;
                }
            }
            return blockBehaviour;
        }

        public override GenericBlock GetBlock(int id)
        {
            GenericBlock genericBlock;
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.Blocks);
            while (true)
            {
                if (enumerator.MoveNext())
                {
                    object current = enumerator.Current;
                    if (!(current is BlockBehaviour))
                    {
                        current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                    }
                    BlockBehaviour blockBehaviour = (BlockBehaviour) current;
                    GenericBlock genericBlock1 = blockBehaviour as GenericBlock;
                    UnityRuntimeServices.Update(enumerator, blockBehaviour);
                    if (genericBlock1 && genericBlock1.BlockID == id)
                    {
                        genericBlock = genericBlock1;
                        break;
                    }
                }
                else
                {
                    genericBlock = null;
                    break;
                }
            }
            return genericBlock;
        }

        public override List<GenericBlock> GetBlocks(int id)
        {
            List<GenericBlock> genericBlocks = new List<GenericBlock>();
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.Blocks);
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                if (!(current is BlockBehaviour))
                {
                    current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                }
                BlockBehaviour blockBehaviour = (BlockBehaviour) current;
                GenericBlock genericBlock = blockBehaviour as GenericBlock;
                UnityRuntimeServices.Update(enumerator, blockBehaviour);
                if (!genericBlock || genericBlock.BlockID != id)
                {
                    continue;
                }
                genericBlocks.Add(genericBlock);
            }
            return genericBlocks;
        }

        private int GetBlockWeight(int blockId)
        {
            int num = new int();
            int num1 = blockId;
            if (num1 == 0)
            {
                num = 5;
            }
            else if (num1 == 2)
            {
                num = 5;
            }
            else if (num1 == 27)
            {
                num = 2;
            }
            else if (num1 != 28)
            {
                num = (num1 != 48 ? 1 : 5);
            }
            else
            {
                num = 3;
            }
            return num;
        }

        public override XData GetMachineData(string key)
        {
            return this.additionalData[key];
        }

        public override BlockBehaviour GetRandomBlock()
        {
            BlockBehaviour blockBehaviour;
            List<BlockBehaviour> blockBehaviours = (!this.simulationClone ? ReferenceMaster.BuildingBlocks : this.simulationBlocks);
            if (blockBehaviours.Count != 0)
            {
                BlockBehaviour item = null;
                while (item == null)
                {
                    int num = UnityEngine.Random.Range(0, blockBehaviours.Count);
                    item = blockBehaviours[num];
                    if (item)
                    {
                        continue;
                    }
                    blockBehaviours.RemoveAt(num);
                }
                blockBehaviour = item;
            }
            else
            {
                blockBehaviour = null;
            }
            return blockBehaviour;
        }

        private bool HasJointConnection(BlockBehaviour blockToFind, bool checkIgnoreID, int ignoreBlockID)
        {
            GenericBlock genericBlock = blockToFind as GenericBlock;
            if (genericBlock.BlockID != 7)
            {
                Joint component = blockToFind.GetComponent<Joint>();
                if (component && component.connectedBody)
                {
                    BlockBehaviour blockBehaviour = component.connectedBody.GetComponent<BlockBehaviour>();
                    if (blockBehaviour)
                    {
                        int instanceID = blockBehaviour.GetInstanceID();
                        if (this.ignoreBlocks.ContainsKey(instanceID) || checkIgnoreID && instanceID == ignoreBlockID)
                        {
                            return false;
                        }
                        return true;
                    }
                }
            }
            else
            {
                Joint[] components = blockToFind.GetComponents<ConfigurableJoint>();
                if (components.Length == 2)
                {
                    BlockBehaviour component1 = components[1].connectedBody.GetComponent<BlockBehaviour>();
                    int num = component1.GetInstanceID();
                    if (this.ignoreBlocks.ContainsKey(num) || checkIgnoreID && num == ignoreBlockID)
                    {
                        BlockBehaviour blockBehaviour1 = components[0].connectedBody.GetComponent<BlockBehaviour>();
                        int instanceID1 = blockBehaviour1.GetInstanceID();
                        if (this.ignoreBlocks.ContainsKey(instanceID1) || checkIgnoreID && instanceID1 == ignoreBlockID)
                        {
                            goto Label2;
                        }
                        return true;
                    }
                    else
                    {
                        return true;
                    }
                }
                Label2:
                ;
            }
            return false;
        }

        public override IEnumerator LoadMachineInfo(MachineInfo info)
        {
            Reset();
            Name = info.Name;
            BuildingMachine.rotation = info.Rotation;
            BuildingMachine.position = info.Position;
            additionalData = info.MachineData.ReadAll().ToDictionary<XData, string>(new Func<XData, string>(LoadMachineInfo));
            BlockBehaviour block = null;
            IEnumerator iterator = UnityRuntimeServices.GetEnumerator(info.Blocks);
            while (iterator.MoveNext())
                        {
                object current = iterator.Current;
                if (!(current is BlockInfo))
                {
                    current = RuntimeServices.Coerce(current, typeof(BlockInfo));
                }
                var blockInfo = (BlockInfo)current;
                block = AddBlock(blockInfo);
                UnityRuntimeServices.Update(iterator, blockInfo);
                if (block?.VisualController == null)
                            {
                    continue;
                }
                block.VisualController.PlaceFromBlockInfo(blockInfo);
                UnityRuntimeServices.Update(iterator, blockInfo);
            }
            yield return new WaitForFixedUpdate();
            addPiece.UpdateMiddleOfObject();
        }

        public override void LoadMachineInfoDifference(MachineInfo info)
        {
            bool flag;
            this.Name = info.Name;
            this.BuildingMachine.position = info.Position;
            this.BuildingMachine.rotation = info.Rotation;
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(new List<BlockBehaviour>(this.BuildingBlocks));
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                if (!(current is BlockBehaviour))
                {
                    current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                }
                BlockBehaviour position = (BlockBehaviour) current;
                flag = false;
                IEnumerator enumerator1 = UnityRuntimeServices.GetEnumerator(info.Blocks);
                while (enumerator1.MoveNext())
                {
                    object obj = enumerator1.Current;
                    if (!(obj is BlockInfo))
                    {
                        obj = RuntimeServices.Coerce(obj, typeof (BlockInfo));
                    }
                    BlockInfo blockInfo = (BlockInfo) obj;
                    if (position.Guid != blockInfo.Guid)
                    {
                        continue;
                    }
                    position.transform.localPosition = blockInfo.Position;
                    UnityRuntimeServices.Update(enumerator, position);
                    UnityRuntimeServices.Update(enumerator1, blockInfo);
                    position.transform.localRotation = blockInfo.Rotation;
                    UnityRuntimeServices.Update(enumerator, position);
                    UnityRuntimeServices.Update(enumerator1, blockInfo);
                    position.VisualController.UpdateVisFromBlockInfo(blockInfo);
                    UnityRuntimeServices.Update(enumerator, position);
                    UnityRuntimeServices.Update(enumerator1, blockInfo);
                    flag = true;
                    break;
                }
                if (flag)
                {
                    continue;
                }
                this.RemoveBlock(position);
                UnityRuntimeServices.Update(enumerator, position);
            }
            BlockBehaviour blockBehaviour = null;
            IEnumerator enumerator2 = UnityRuntimeServices.GetEnumerator(info.Blocks);
            while (enumerator2.MoveNext())
            {
                object current1 = enumerator2.Current;
                if (!(current1 is BlockInfo))
                {
                    current1 = RuntimeServices.Coerce(current1, typeof (BlockInfo));
                }
                BlockInfo blockInfo1 = (BlockInfo) current1;
                flag = false;
                IEnumerator enumerator3 = UnityRuntimeServices.GetEnumerator(new List<BlockBehaviour>(this.BuildingBlocks));
                while (enumerator3.MoveNext())
                {
                    object obj1 = enumerator3.Current;
                    if (!(obj1 is BlockBehaviour))
                    {
                        obj1 = RuntimeServices.Coerce(obj1, typeof (BlockBehaviour));
                    }
                    BlockBehaviour blockBehaviour1 = (BlockBehaviour) obj1;
                    if (blockInfo1.Guid != blockBehaviour1.Guid)
                    {
                        continue;
                    }
                    flag = true;
                    break;
                }
                if (flag)
                {
                    continue;
                }
                blockBehaviour = this.AddBlock(blockInfo1);
                UnityRuntimeServices.Update(enumerator2, blockInfo1);
                if (blockBehaviour?.VisualController == null)
                {
                    continue;
                }
                blockBehaviour.VisualController.PlaceFromBlockInfo(blockInfo1);
                UnityRuntimeServices.Update(enumerator2, blockInfo1);
            }
        }

        public override void Main()
        {
        }

        public override IEnumerator NotifyJointBreak(BlockBehaviour disconnectedBlock, int ignoreBlockID)
        {
            if (enableAdaptiveCam)
            {
                if (notifyWaitWrite)
                {
                    yield return null;
                }
                else
                {
                    notifyList.Enqueue(disconnectedBlock);
                    if (notifyList.Count <= 1)
                    {
                        yield return new WaitForSeconds(notifyDelay);
                        notifyWaitWrite = true;
                        if (!ignoreDisconnectedBlocks)
                        {
                            addPiece.UpdateMiddleOfObject();
                        }
                        else
                        {
                            var iterator = UnityRuntimeServices.GetEnumerator(notifyList);
                            while (iterator.MoveNext())
                            {
                                object current = iterator.Current;
                                if (!(current is BlockBehaviour))
                                {
                                    current = RuntimeServices.Coerce(current, typeof(BlockBehaviour));
                                }
                                var block = (BlockBehaviour)current;
                                FindDisconnectedBlocks(block, ignoreBlockID);
                                UnityRuntimeServices.Update(iterator, block);
                            }
                            FindEndPoints();
                        }
                        notifyList.Clear();
                        notifyWaitWrite = false;
                        cameraUpdate = true;
                        yield return null;
                    }
                }
            }
        }

        public override void RegisterBlock(BlockBehaviour block)
        {
            if (!ReferenceMaster.BuildingBlocks.Contains(block))
            {
                ReferenceMaster.BuildingBlocks.Add(block);
            }
            addPiece.UpdateBlockAndMassText(this.Mass, StatMaster.BlockCount);
        }

        public override void RemoveBlock(BlockBehaviour block)
        {
            this.UnregisterBlock(block);
            DestroyImmediate(block.gameObject);
        }

        public override void RemoveBlock(Transform transform)
        {
            BlockBehaviour block = this.GetBlock(transform);
            if (block == null)
            {
                Debug.LogWarning("Specified transform is not a block.");
            }
            else
            {
                this.UnregisterBlock(block);
                DestroyImmediate(block.gameObject);
            }
        }

        public override void RemoveBlockEndOfFrame(Transform transform)
        {
            BlockBehaviour block = this.GetBlock(transform);
            if (block == null)
            {
                Debug.LogWarning("Specified transform is not a block.");
            }
            else
            {
                this.UnregisterBlock(block);
                Destroy(block.gameObject);
            }
        }

        public override void Reset()
        {
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(new List<BlockBehaviour>(this.BuildingBlocks));
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                if (!(current is BlockBehaviour))
                {
                    current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                }
                BlockBehaviour blockBehaviour = (BlockBehaviour) current;
                if (blockBehaviour == null)
                {
                    continue;
                }
                blockBehaviour.transform.name = "destroyed";
                UnityRuntimeServices.Update(enumerator, blockBehaviour);
                this.RemoveBlock(blockBehaviour);
                UnityRuntimeServices.Update(enumerator, blockBehaviour);
            }
            this.Name = "Unnamed";
            this.BuildingMachine.position = Vector3.up*(float) 6;
            this.BuildingMachine.rotation = Quaternion.identity;
            SingleInstance<AddPiece>.Instance.checkPinBlocks = false;
        }

        public override void SaveMachineData(XDataHolder data)
        {
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.additionalData.Values);
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                if (!(current is XData))
                {
                    current = RuntimeServices.Coerce(current, typeof (XData));
                }
                XData xDatum = (XData) current;
                data.Write(xDatum);
                UnityRuntimeServices.Update(enumerator, xDatum);
            }
        }

        public override Vector3 SimulationMiddlePosition()
        {
            Vector3 vector3;
            Vector3 item = this.simulationBlocks[0].transform.position;
            if (!Mathf.Approximately(item.sqrMagnitude, this.lastMachinePosition.sqrMagnitude))
            {
                this.lastMachinePosition = item;
                if (this.ignoreDisconnectedBlocks)
                {
                    if (this.machineEndPoints.Count == 0)
                    {
                        vector3 = this.cameraAverage;
                        return vector3;
                    }
                    Vector3 vector31 = Vector3.zero;
                    int num = 0;
                    IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.machineEndPoints);
                    while (enumerator.MoveNext())
                    {
                        object current = enumerator.Current;
                        if (!(current is BlockBehaviour))
                        {
                            current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                        }
                        BlockBehaviour blockBehaviour = (BlockBehaviour) current;
                        if (!blockBehaviour)
                        {
                            continue;
                        }
                        Vector3 vector32 = blockBehaviour.transform.position;
                        UnityRuntimeServices.Update(enumerator, blockBehaviour);
                        if (!this.useEndPointWeights || !this.endPointWeights.ContainsKey(blockBehaviour))
                        {
                            vector31 = vector31 + blockBehaviour.transform.position;
                            UnityRuntimeServices.Update(enumerator, blockBehaviour);
                            num++;
                        }
                        else
                        {
                            int item1 = this.endPointWeights[blockBehaviour];
                            UnityRuntimeServices.Update(enumerator, blockBehaviour);
                            vector31 = vector31 + (vector32*(float) item1);
                            num = num + item1;
                        }
                    }
                    if (num > 0)
                    {
                        vector31 = vector31/(float) num;
                        this.cameraAverage = vector31;
                    }
                }
                else if (!this.cameraUpdate)
                {
                    this.cameraAverage = item - this.cameraOffset;
                }
                else
                {
                    this.cameraAverage = this.MiddlePosition;
                    this.cameraOffset = item - this.cameraAverage;
                    this.cameraUpdate = false;
                }
                vector3 = this.cameraAverage;
            }
            else
            {
                vector3 = this.cameraAverage;
            }
            return vector3;
        }

        public override IEnumerator StartSimulation()
        {
            machine.gameObject.SetActive(false);
            simulationClone = Instantiate<Transform>(machine).transform;
            simulationClone.SetParent(transform);
            simulationClone.gameObject.name = "Simulation Machine";
            simulationClone.gameObject.SetActive(true);
            simulationClone.position = machine.position;
            simulationBlocks = new List<BlockBehaviour>();
            var mouseOrbit = SingleInstanceFindOnly<MouseOrbit>.Instance;
            var lookForBlock = mouseOrbit.targetIsBlock;
            for(int i = 0; i < ReferenceMaster.BuildingBlocks.Count; i++)
                        {
                var newBlock = simulationClone.transform.GetChild(i).GetComponent<BlockBehaviour>();
                var oldBlock = ReferenceMaster.BuildingBlocks[i];
                newBlock.VisualController.PlaceFromBlock(oldBlock);
                newBlock.VisualController.SetNormalWait(2);
                if (lookForBlock && oldBlock.transform == mouseOrbit.target)
                            {
                    lookForBlock = false;
                    mouseOrbit.target = newBlock.transform;
                }
                var data = new XDataHolder()
                {
                    WasSimulationStarted = true
                };
                oldBlock.OnSave(data);
                newBlock.OnLoad(data);
                simulationBlocks.Add(newBlock);
            }
            bool flag1 = mouseOrbit.targetIsBlock;
            if (flag1)
            {
                flag1 = !lookForBlock;
            }
            mouseOrbit.targetIsBlock = flag1;
            _isFirstMiddle = true;
            _hasTempMiddle = false;
            _lastDelta = Vector3.zero;
            yield return StartCoroutine(WakePhysicsNextFrame());
            yield return null;
        }

        public override void UnregisterBlock(BlockBehaviour block)
        {
            if (ReferenceMaster.BuildingBlocks.Contains(block))
            {
                ReferenceMaster.BuildingBlocks.Remove(block);
            }
            SingleInstance<AddPiece>.Instance.UpdateBlockAndMassText(this.Mass, StatMaster.BlockCount);
        }

        public override void UnregisterSimulationBlock(BlockBehaviour block)
        {
            if (this.simulationBlocks.Contains(block))
            {
                this.simulationBlocks.Remove(block);
            }
        }

        private void UpdateBounds()
        {
            if (!AddPiece.isSimulating)
            {
                bool flag = true;
                IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(new List<BlockBehaviour>(ReferenceMaster.BuildingBlocks));
                while (enumerator.MoveNext())
                {
                    object current = enumerator.Current;
                    if (!(current is BlockBehaviour))
                    {
                        current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                    }
                    BlockBehaviour blockBehaviour = (BlockBehaviour) current;
                    if (blockBehaviour != null)
                    {
                        MyBounds component = blockBehaviour.GetComponent<MyBounds>();
                        UnityRuntimeServices.Update(enumerator, blockBehaviour);
                        Bounds bounds = new Bounds();
                        if (component)
                        {
                            bounds = component.GetBounds();
                        }
                        if (!component)
                        {
                            continue;
                        }
                        if (!flag)
                        {
                            this.machineBounds.Encapsulate(bounds);
                        }
                        else
                        {
                            this.machineBounds = bounds;
                            flag = false;
                        }
                    }
                    else
                    {
                        ReferenceMaster.BuildingBlocks.Remove(blockBehaviour);
                        UnityRuntimeServices.Update(enumerator, blockBehaviour);
                    }
                }
            }
            else
            {
                Debug.LogError("Can't update bounds during simulation!");
            }
        }

        private void UpdateMass()
        {
            StatMaster.TotalMass = (float) 0;
            StatMaster.BlockCount = this.Blocks.Count;
            IEnumerator enumerator = UnityRuntimeServices.GetEnumerator(this.Blocks);
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                if (!(current is BlockBehaviour))
                {
                    current = RuntimeServices.Coerce(current, typeof (BlockBehaviour));
                }
                BlockBehaviour blockBehaviour = (BlockBehaviour) current;
                if (blockBehaviour)
                {
                    Rigidbody rigidbody = blockBehaviour.Rigidbody;
                    UnityRuntimeServices.Update(enumerator, blockBehaviour);
                    if (!rigidbody)
                    {
                        continue;
                    }
                    StatMaster.TotalMass = StatMaster.TotalMass + rigidbody.mass;
                }
            }
            this.machineMass = StatMaster.TotalMass;
            SingleInstance<AddPiece>.Instance.UpdateBlockAndMassText(StatMaster.TotalMass, StatMaster.BlockCount);
        }

        public override IEnumerator WaitAndTryAddBlockAgain(BlockInfo blockInfo, float waiting)
        {
            yield return new WaitForSeconds(waiting);
            AddBlock(blockInfo, false);
        }

        private IEnumerator WakePhysicsNextFrame()
        {
            var timeSlider = SingleInstance<TimeSlider>.Instance;
            timeSlider.startingSimulation = false;
            timeSlider.wasSimulating = false;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            timeSlider.startingSimulation = true;
            Time.timeScale = (float)0;
            var rigidbodies = simulationClone.GetComponentsInChildren<Rigidbody>();
            foreach (var currentBody in rigidbodies)
            {
                if (!currentBody.GetComponent<FireController>() && !currentBody.GetComponent<StayKinematic>())
                {
                    currentBody.isKinematic = false;
                    currentBody.WakeUp();
                }
            }
            yield return null;
        }
    }
}