using System;
using System.Collections;
using UnityEngine;

namespace BesiegeMP
{

    [Serializable]
    public class AddPieceMP : MonoBehaviour
    {
        private int _currentBlockType;

        [NonSerialized]
        public bool disableBlockPlacement;

        [NonSerialized]
        public bool ghostEnabled;

        [NonSerialized]
        public float timeTaken;

        [HideInInspector]
        public bool canAdd;

        public AudioSource clickSound;

        public Transform middleOfObject;

        public Transform activeInstance;

        public RandomSoundController deleteSound;

        public System.Collections.Generic.List<Transform> sendSimulateMessage;

        public MachineCenterOfMass comCode;

        public RandomSoundController woodHitAudioController;

        public SymmetryController symmetryController;

        [HideInInspector]
        public float floorHeight;

        private MachineObjectTrackerMP machineTracker;

        private float deleteDownTime;

        private Transform lastObject;

        private BlockBehaviour lastBlock;

        private Vector3 lastMiddleOfObject;

        internal MachineMP machine;

        public BlockBehaviour LastBlock
        {
            get
            {
                return this.lastBlock;
            }
        }

        public string Name
        {
            get
            {
                return "_BUILDER";
            }
        }

        public AddPieceMP()
        {
            this.sendSimulateMessage = new System.Collections.Generic.List<Transform>();
        }

        public void AddBlock(Transform block, int BlockType)
        {
            Vector3 vector3 = block.position;
            Quaternion quaternion = block.rotation;
            this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(vector3, quaternion, BlockType);
            this.lastObject = this.lastBlock.transform;
            this.UpdateMiddleOfObject();
        }

        public void AddBlockType(Transform block, int BlockType, Guid BlockHit)
        {
            if (this.AddBlockTypeNoSound(block, BlockType, BlockHit))
            {
                this.woodHitAudioController.Stop();
                this.woodHitAudioController.Play();
            }
        }

        public bool AddBlockTypeNoSound(Transform block, int BlockType, Guid BlockHit)
        {
            bool flag;
            if (!disableBlockPlacement && BlockType != 0)
            {
                bool flag1 = false;
                if (BlockType == 19)
                {
                    AddHinge(block, BlockType, BlockHit);
                    flag1 = true;
                }
                else if (BlockType == 7)
                {
                    AddBrace(block, BlockType, BlockHit);
                    flag1 = false;
                }
                else if (BlockType == 9 || BlockType == 45)
                {
                    this.AddSpring(block, BlockType);
                    flag1 = false;
                }
                else if (BlockType == 5)
                {
                    this.AddHinge(block, BlockType, BlockHit);
                    flag1 = true;
                }
                else if (BlockType != 8)
                {
                    this.AddBlock(block, BlockType);
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
                    machine.Active().UndoSystem.Snapshot();
                }
                flag = true;
            }
            else
            {
                flag = false;
            }
            return flag;
        }

        public void AddBrace(Transform block, int BlockType, Guid blockHit)
        {
            this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(block.position, block.rotation, BlockType);
            this.lastObject = this.lastBlock.transform;
            this.lastObject.gameObject.SendMessage("setJointTo", GetBlockByGUID(blockHit).GetComponent<Rigidbody>(), SendMessageOptions.DontRequireReceiver);
        }

        public void AddHinge(Transform block, int BlockType, Guid blockHit)
        {
            this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(block.position, block.rotation, BlockType);
            this.lastObject = this.lastBlock.transform;
            HingeJoint component = this.lastObject.GetComponent<HingeJoint>();
            if (component != null)
            {
                component.connectedBody = GetBlockByGUID(blockHit).GetComponent<Rigidbody>();
            }
        }

        public void AddSpring(Transform block, int BlockType)
        {
            this.lastBlock = this.machineTracker.ActiveMachine.AddBlockGlobal(block.position, block.rotation, BlockType);
            this.lastObject = this.lastBlock.transform;
        }

        protected void Awake()
        {
            this.machineTracker = UnityEngine.Object.FindObjectOfType<MachineObjectTrackerMP>();
        }

        public BlockBehaviour GetBlockByGUID(Guid GUID)
        {
            return machine.Active().Blocks.Find(behaviour => behaviour.Guid == GUID);
        }

        public IEnumerator RemoveBlock(Guid GUID)
        {
            var block = GetBlockByGUID(GUID);
            machine.UnregisterBlock(block);
            Destroy(block.gameObject);
            deleteSound.Stop();
            deleteSound.Play();
            machine.UndoSystem.Snapshot();
            yield return null;
            UpdateMiddleOfObject();
            yield return null;
        }

        public void SendSimulateMessage(bool @bool)
        {
            foreach (Transform item in this.sendSimulateMessage)
            {
                if (@bool)
                {
                    item.gameObject.SendMessage("OnSimulate", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    item?.gameObject.SendMessage("OnStopSimulate", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        public IEnumerator Simulate()
        {
            SendSimulateMessage(StatMaster.isSimulating);
            symmetryController.ClearSymGameObjects();
            if (!StatMaster.isSimulating)
            {
                machine.Active().EndSimulation();
                middleOfObject.position = lastMiddleOfObject;
                yield return null;
            }
            else
            {
                yield return StartCoroutine(machine.Active().StartSimulation());
            }
        }

        public void UpdateCOM()
        {
            this.comCode.GetCOM(this.machineTracker.BuildingMachine);
        }

        public void UpdateMiddleOfObject()
        {
            this.UpdateMiddleOfObject(false);
        }

        public void UpdateMiddleOfObject(bool fullCalculationForced)
        {
            this.UpdateCOM();
            this.middleOfObject.position = machine.Active().CalculateMiddle(fullCalculationForced);
            this.lastMiddleOfObject = this.middleOfObject.position;
        }

        [Serializable]
        public class DirAnglePair
        {
            public Vector3 dir;

            public float angle;
        }

        public static class PlacementOffset
        {
            [NonSerialized] public static Vector3 position;

            [NonSerialized] public static Quaternion rotation;

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
}
