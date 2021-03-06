﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocketLeagueReplayParser
{
    public class ActorStateProperty
    {
        public Int32 PropertyId { get; private set; }
        public Int32 MaxPropertyId { get; private set; }
        public string PropertyName { get; private set; }
        public List<object> Data { get; private set; }
        public List<bool> KnownBits { get; private set; }

        public bool IsComplete { get; private set; }

        public static ActorStateProperty Deserialize(IClassNetCache classMap, IDictionary<int, string> objectIndexToName, BitReader br)
        {
            var asp = new ActorStateProperty();
            var startPosition = br.Position;

            var maxPropId = classMap.MaxPropertyId;
            //var idBitLen = Math.Floor(Math.Log10(maxPropId) / Math.Log10(2)) + 1;

            var className = objectIndexToName[classMap.ObjectIndex];
            asp.PropertyId = br.ReadInt32Max(maxPropId + 1);// br.ReadInt32FromBits((int)idBitLen);
            asp.MaxPropertyId = maxPropId;
            asp.PropertyName = objectIndexToName[classMap.GetProperty(asp.PropertyId).Index];
            asp.Data = new List<object>();
            try
            {
                switch (asp.PropertyName)
                {
                    case "TAGame.GameEvent_TA:ReplicatedStateIndex":
                        asp.Data.Add(br.ReadInt32Max(140)); // number is made up, I dont know the max yet
                        asp.IsComplete = true;
                        break;
                    case "TAGame.RBActor_TA:ReplicatedRBState":
                        
                        asp.Data.Add(br.ReadBit());
                        asp.Data.Add(Vector3D.Deserialize2(20, br));

                        var rot = Vector3D.DeserializeFixed(br);
                        asp.Data.Add(rot);
                        // Sometimes these two vectors are missing?
                        // Wild guess: They're momentum vectors, and only there when moving?
                        // Well, how do I know they're moving without momentum vectors... hm.
                        if (!(rot.X < -1 && rot.Y < -1 && rot.Z < -1))
                        {
                            asp.Data.Add(Vector3D.Deserialize2(20, br));
                            asp.Data.Add(Vector3D.Deserialize2(20, br));
                        }

                        asp.IsComplete = true;
                        break;
                    case "TAGame.Team_TA:GameEvent":
                    case "TAGame.CrowdActor_TA:ReplicatedOneShotSound":
                    case "TAGame.CrowdManager_TA:ReplicatedGlobalOneShotSound":
                    case "Engine.Actor:Owner":
                    case "TAGame.GameEvent_Soccar_TA:RoundNum":
                    case "Engine.GameReplicationInfo:GameClass":
                    case "TAGame.GameEvent_TA:BotSkill":
                    case "Engine.PlayerReplicationInfo:Team":
                    case "TAGame.CrowdManager_TA:GameEvent":
                    case "Engine.Pawn:PlayerReplicationInfo":
                    //case "TAGame.VehiclePickup_TA:ReplicatedPickupData":
                        asp.Data.Add(br.ReadBit()); 
                        asp.Data.Add(br.ReadInt32());
                        asp.IsComplete = true;
                        break;
                    case "TAGame.CarComponent_TA:Vehicle":
                        // 110101111 // TAGame.CarComponent_Jump_TA
                        // 100111111 // TAGame.CarComponent_FlipCar_TA
                        asp.Data.Add(br.ReadBit());
                        if (className == "TAGame.CarComponent_Jump_TA"
                            || className == "TAGame.CarComponent_FlipCar_TA"
                            || className == "TAGame.CarComponent_Boost_TA"
                            || className == "TAGame.CarComponent_Dodge_TA"
                            || className == "TAGame.CarComponent_DoubleJump_TA")
                        {
                            asp.Data.Add(br.ReadInt32());
                        }
                        else
                        {
                            asp.Data.Add(br.ReadByte());
                        }
                        asp.IsComplete = true;
                        break;
                    case "Engine.PlayerReplicationInfo:PlayerName":
                    case "Engine.GameReplicationInfo:ServerName":
                        asp.Data.Add(br.ReadString());
                        asp.IsComplete = true;
                        break;
                    case "TAGame.GameEvent_Soccar_TA:SecondsRemaining":
                    case "TAGame.GameEvent_TA:ReplicatedGameStateTimeRemaining":
                    case "TAGame.CrowdActor_TA:ReplicatedCountDownNumber":
                    case "TAGame.CrowdActor_TA:ModifiedNoise":
                    case "TAGame.GameEvent_Team_TA:MaxTeamSize":
                        asp.Data.Add(br.ReadInt32());
                        asp.IsComplete = true;
                        break;
                    case "TAGame.VehiclePickup_TA:ReplicatedPickupData":
                        // 1011101000000000000000000000000001
                        // 0111111111111111111111111111111110
                        // 1111001000000000000000000000000001
                        // 1000001000000000000000000000000001
                        // 1111110000000000000000000000000001
                        // 1101110000000000000000000000000001
                        // 111111111
                        // 100000001
                        // 101001111

                        var bit1 = br.ReadBit();
                        var byt = br.ReadByte();
                        var bit2 = (byt & 0x80) > 0;
                        if (bit1 == bit2)
                        {
                            asp.Data.Add(bit1);
                            asp.Data.Add(byt);
                        }
                        else
                        {
                            asp.Data.Add(bit1);
                            var bytes = new byte[4];
                            bytes[0] = byt;
                            bytes[1] = br.ReadByte();
                            bytes[2] = br.ReadByte();
                            bytes[3] = br.ReadByte();
                            asp.Data.Add(BitConverter.ToInt32(bytes, 0));
                            asp.Data.Add(br.ReadBit());
                        }
                        asp.IsComplete = true;
                        break;
                    case "Engine.Actor:bNetOwner":
                    case "Engine.Actor:bBlockActors":
                        // this doesnt look right...
                        asp.Data.Add(br.ReadBit());
                        asp.Data.Add(br.ReadBit());
                        asp.Data.Add(br.ReadBit());
                        asp.IsComplete = true;
                        break;
                    case "Engine.Pawn:DrivenVehicle":
                        asp.Data.Add(br.ReadInt32());
                        asp.Data.Add(br.ReadBit());
                        asp.Data.Add(br.ReadBit());
                        asp.IsComplete = true;
                        break;
                    case "Engine.Actor:DrawScale":

                        // Might be more properties in this sample data
                        // 100011000000000000010000000001001110010100000001000000000000000000000000000000110000000000000000000100000000010000000001000110000000000010101
                        // 1011010000000100010000000011001110010100000010000000000000000000000000000011100000000000000001001000000000010000000000100110101100000000101010
                        // 1011010000000011110000000001111111000100000010000000000000000000000000000001100000000000000000101000000000001000000000001110111111100000000101010
                        break;
                    case "Engine.PlayerReplicationInfo:Ping":
                    case "TAGame.Vehicle_TA:ReplicatedSteer":
                    case "TAGame.Vehicle_TA:ReplicatedThrottle":
                    case "TAGame.PRI_TA:CameraYaw":
                    case "TAGame.PRI_TA:CameraPitch":
                        asp.Data.Add(br.ReadByte());
                        asp.IsComplete = true;
                        break;
                    case "Engine.Actor:Location":
                    case "TAGame.CarComponent_Dodge_TA:DodgeTorque":
                        asp.Data.Add(Vector3D.Deserialize(br));
                        asp.IsComplete = true;
                        break;
                    
                    case "Engine.Actor:bCollideWorld":
                    case "Engine.PlayerReplicationInfo:bReadyToPlay":
                    case "TAGame.Vehicle_TA:bReplicatedHandbrake":
                    case "TAGame.Vehicle_TA:bDriving":
                        //asp.Data.Add(Vector3D.Deserialize(5, br));
                        asp.Data.Add(br.ReadBit());
                        asp.IsComplete = true;
                        break;
                    case "TAGame.PRI_TA:bUsingBehindView":
                    case "TAGame.PRI_TA:bUsingSecondaryCamera":
                        asp.Data.Add(br.ReadBit());
                        asp.IsComplete = true;
                        break;
                    case "TAGame.CarComponent_TA:ReplicatedActive":
                        // example data
                        // 0111111111111111111111111111111110

                        asp.Data.Add(br.ReadByte());
                        /*
                        asp.Data.Add(br.ReadBit());
                        if ( (bool)asp.Data[0])
                        {
                            asp.Data.Add(br.ReadInt32FromBits(7));
                        }*/
                        asp.IsComplete = true;
                        break;
                    case "Engine.Actor:Role":
                        asp.Data.Add(br.ReadInt32FromBits(11));
                        asp.IsComplete = true;
                        break;
                    /*case "Engine.Actor:RelativeRotation": //SWAG
                        asp.Data.Add(br.ReadBit());
                        asp.Data.Add(br.ReadByte());
                        asp.Data.Add(br.ReadInt32());
                        asp.IsComplete = true;
                        break;*/
                    case "Engine.PlayerReplicationInfo:UniqueId":
                        asp.Data.Add(br.ReadBit());
                        asp.Data.Add(br.ReadByte());
                        asp.IsComplete = true;
                        break;
                }
            }
            catch(Exception)
            {
                asp.Data.Add("FAILED");
            }
            finally
            {
                asp.KnownBits = br.GetBits(startPosition, br.Position - startPosition);
            }

            return asp;
        }

        private static List<object> ReadData(int numBytes, int numBits, BitReader br)
        {
            List<object> data = new List<object>();
            for (int i = 0; i < numBytes; ++i)
            {
                data.Add(br.ReadByte());
            }
            for (int i = 0; i < numBits; ++i)
            {
                data.Add(br.ReadBit());
            }
            return data;
        }

        public string ToDebugString()
        {
            var s = string.Format("Property: ID {0} Name {1}\r\n", PropertyId, PropertyName);
            s += "    Max Prop Id: " + MaxPropertyId.ToString() + "\r\n";
            s += "    Data: " + string.Join(", ", Data) + "\r\n";
            
            // You know you should really functionify this, right?
            // yeah.
            if (KnownBits != null && KnownBits.Count > 0)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < KnownBits.Count; ++i)
                {
                    sb.Append((KnownBits[i] ? 1 : 0).ToString());
                }

                s += string.Format("    KnownBits: {0}\r\n", sb.ToString());
            }
            return s;
            //return string.Join(", ", Data);

        }
    }
}
