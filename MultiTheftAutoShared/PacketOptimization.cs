﻿using System;
using System.Collections.Generic;
using static System.BitConverter;

namespace GTANetworkShared
{
    public static class PacketOptimization
    {
        #region Write Operations

        public static byte[] WritePureSync(PedData data)
        {
            List<byte> byteArray = new List<byte>();

            // Write player's nethandle.
            if (data.NetHandle.HasValue)
            {
                byteArray.AddRange(GetBytes(data.NetHandle.Value));
            }
            else
            {
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
            }

            // Write the flag
            byteArray.AddRange(GetBytes((short)data.Flag.Value));

            // Write player's position, rotation, and velocity
            byteArray.AddRange(GetBytes(data.Position.X));
            byteArray.AddRange(GetBytes(data.Position.Y));
            byteArray.AddRange(GetBytes(data.Position.Z));

            // Only send roll & pitch if we're ragdolling.
            if (CheckBit(data.Flag.Value, PedDataFlags.Ragdoll))
            {
                byteArray.AddRange(GetBytes(data.Quaternion.X));
                byteArray.AddRange(GetBytes(data.Quaternion.Y));
            }

            byteArray.AddRange(GetBytes(data.Quaternion.Z));

            // optimize velocity to save 6 bytes
            byteArray.AddRange(GetBytes(CompressSingle(data.Velocity.X)));
            byteArray.AddRange(GetBytes(CompressSingle(data.Velocity.Y)));
            byteArray.AddRange(GetBytes(CompressSingle(data.Velocity.Z)));
            
            // Write player health, armor and walking speed
            byteArray.Add(data.PlayerHealth.Value);
            byteArray.Add(data.PedArmor.Value);
            byteArray.Add(data.Speed.Value);

            // TODO: Move shooting into it's own packet.
            // Are we shooting?
            if (CheckBit(data.Flag.Value, PedDataFlags.Aiming) ||
                CheckBit(data.Flag.Value, PedDataFlags.Shooting))
            {
                // Write current weapon hash.
                byteArray.AddRange(GetBytes(data.WeaponHash.Value));
                
                // Aim coordinates
                byteArray.AddRange(GetBytes(data.AimCoords.X));
                byteArray.AddRange(GetBytes(data.AimCoords.Y));
                byteArray.AddRange(GetBytes(data.AimCoords.Z));
            }
            
            return byteArray.ToArray();
        }

        public static byte[] WriteLightSync(PedData data)
        {
            List<byte> byteArray = new List<byte>();

            // Write player's nethandle.
            if (data.NetHandle.HasValue)
            {
                byteArray.AddRange(GetBytes(data.NetHandle.Value));
            }
            else
            {
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
            }

            // Write player model
            byteArray.AddRange(GetBytes(data.PedModelHash.Value));

            // Write current weapon hash.
            byteArray.AddRange(GetBytes(data.WeaponHash.Value));

            // Write player's latency
            if (data.Latency.HasValue)
            {
                var latency = data.Latency.Value*1000;
                byteArray.AddRange(GetBytes((short) latency));
            }

            return byteArray.ToArray();
        }

        public static byte[] WritePureSync(VehicleData data)
        {
            List<byte> byteArray = new List<byte>();

            // Write player's nethandle.
            if (data.NetHandle.HasValue)
            {
                byteArray.AddRange(GetBytes(data.NetHandle.Value));
            }
            else
            {
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
            }

            // Write player health and armor
            byteArray.Add(data.PlayerHealth.Value);
            byteArray.Add(data.PedArmor.Value);

            // Write the flag
            byteArray.Add(data.Flag.Value);

            if (CheckBit(data.Flag.Value, VehicleDataFlags.Aiming) ||
                CheckBit(data.Flag.Value, VehicleDataFlags.MountedWeapon) ||
                CheckBit(data.Flag.Value, VehicleDataFlags.Shooting))
            {
                // Write the gun model
                byteArray.AddRange(GetBytes(data.WeaponHash.Value));

                // Write the aiming point
                byteArray.AddRange(GetBytes(data.AimCoords.X));
                byteArray.AddRange(GetBytes(data.AimCoords.Y));
                byteArray.AddRange(GetBytes(data.AimCoords.Z));
            }
            
            // Are we the driver?
            if (CheckBit(data.Flag.Value, VehicleDataFlags.Driver))
            {
                // Write vehicle position, rotation and velocity
                byteArray.AddRange(GetBytes(data.Position.X));
                byteArray.AddRange(GetBytes(data.Position.Y));
                byteArray.AddRange(GetBytes(data.Position.Z));

                byteArray.AddRange(GetBytes(data.Quaternion.X));
                byteArray.AddRange(GetBytes(data.Quaternion.Y));
                byteArray.AddRange(GetBytes(data.Quaternion.Z));


                // Compress velocity to save 6 bytes
                byteArray.AddRange(GetBytes(CompressSingle(data.Velocity.X)));
                byteArray.AddRange(GetBytes(CompressSingle(data.Velocity.Y)));
                byteArray.AddRange(GetBytes(CompressSingle(data.Velocity.Z)));

                // Write vehicle health
                byteArray.AddRange(GetBytes((short) ((int) data.VehicleHealth.Value)));

                // Write engine stuff
                byte rpm = (byte) (data.RPM.Value*byte.MaxValue);

                float angle = Extensions.Clamp(data.Steering.Value, -45f, 45f);
                angle += 45f;
                byte angleCrammed = (byte) ((angle/90f)*byte.MaxValue);

                byteArray.Add(rpm);
                byteArray.Add(angleCrammed);
            }

            return byteArray.ToArray();
        }

        public static byte[] WriteLightSync(VehicleData data)
        {
            List<byte> byteArray = new List<byte>();

            // Write player's nethandle.
            if (data.NetHandle.HasValue)
            {
                byteArray.AddRange(GetBytes(data.NetHandle.Value));
            }
            else
            {
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
                byteArray.Add(0x00);
            }

            // Write player model
            byteArray.AddRange(GetBytes(data.PedModelHash.Value));

            // Write his vehicle handle
            byteArray.AddRange(GetBytes(data.VehicleHandle.Value));

            // Write his seat
            byteArray.Add((byte) data.VehicleSeat.Value);

            // Write the gun model
            byteArray.AddRange(GetBytes(data.WeaponHash.Value));


            // If he has a trailer attached, write it's position. (Maybe we can use his pos & rot to calculate it serverside?)
            if (data.Trailer != null)
            {
                byteArray.Add(0x01);
                byteArray.AddRange(GetBytes(data.Trailer.X));
                byteArray.AddRange(GetBytes(data.Trailer.Y));
                byteArray.AddRange(GetBytes(data.Trailer.Z));
            }
            else
            {
                byteArray.Add(0x00);
            }

            // Write player's latency
            if (data.Latency.HasValue)
            {
                var latency = data.Latency.Value * 1000;
                byteArray.AddRange(GetBytes((short)latency));
            }

            return byteArray.ToArray();
        }

        public static byte[] WriteBasicSync(int netHandle, Vector3 position)
        {
            List<byte> byteArray = new List<byte>();

            // write the player nethandle
            byteArray.AddRange(GetBytes(netHandle));

            // Write his position
            byteArray.AddRange(GetBytes(position.X));
            byteArray.AddRange(GetBytes(position.Y));
            byteArray.AddRange(GetBytes(position.Z));

            return byteArray.ToArray();
        }

        #endregion

        #region Read Operations

        public static PedData ReadPurePedSync(byte[] array)
        {
            var data = new PedData();
            var r = new BitReader(array);

            // Read player nethandle
            data.NetHandle = r.ReadInt32();

            // Read the flag
            data.Flag = r.ReadInt16();

            // Read player position, rotation and velocity
            Vector3 position = new Vector3();
            Vector3 rotation = new Vector3();
            Vector3 velocity = new Vector3();

            position.X = r.ReadSingle();
            position.Y = r.ReadSingle();
            position.Z = r.ReadSingle();

            // Only read pitchand roll if he's ragdolling
            if (CheckBit(data.Flag.Value, PedDataFlags.Ragdoll))
            {
                rotation.X = r.ReadSingle();
                rotation.Y = r.ReadSingle();
            }

            rotation.Z = r.ReadSingle();

            velocity.X = DecompressSingle(r.ReadUInt16());
            velocity.Y = DecompressSingle(r.ReadUInt16());
            velocity.Z = DecompressSingle(r.ReadUInt16());

            data.Position = position;
            data.Quaternion = rotation;
            data.Velocity = velocity;

            // Read health, armor and speed
            data.PlayerHealth = r.ReadByte();
            data.PedArmor = r.ReadByte();
            data.Speed = r.ReadByte();

            // Is the player shooting?
            if (CheckBit(data.Flag.Value, PedDataFlags.Aiming) ||
                CheckBit(data.Flag.Value, PedDataFlags.Shooting))
            {
                // read gun model
                data.WeaponHash = r.ReadInt32();

                // read where is he aiming
                Vector3 aimPoint = new Vector3();

                aimPoint.X = r.ReadSingle();
                aimPoint.Y = r.ReadSingle();
                aimPoint.Z = r.ReadSingle();

                data.AimCoords = aimPoint;
            }

            return data;
        }

        public static PedData ReadLightPedSync(byte[] array)
        {
            var data = new PedData();
            var r = new BitReader(array);

            // Read player nethandle
            data.NetHandle = r.ReadInt32();
            
            // Read player model
            data.PedModelHash = r.ReadInt32();

            // Read weapon model
            data.WeaponHash = r.ReadInt32();

            // If we can, read latency

            if (r.CanRead(2))
            {
                var latency = r.ReadInt16();

                data.Latency = latency/1000f;
            }
            
            return data;
        }

        public static VehicleData ReadPureVehicleSync(byte[] array)
        {
            var data = new VehicleData();
            var r = new BitReader(array);

            // Read player nethandle
            data.NetHandle = r.ReadInt32();

            // read health values
            data.PlayerHealth = r.ReadByte();
            data.PedArmor = r.ReadByte();

            // read flag
            data.Flag = r.ReadByte();

            // If we're shooting/aiming, read gun stuff
            if (CheckBit(data.Flag.Value, VehicleDataFlags.Shooting) ||
                CheckBit(data.Flag.Value, VehicleDataFlags.Aiming))
            {
                // read gun model
                data.WeaponHash = r.ReadInt32();

                // read aim coordinates
                Vector3 aimCoords = new Vector3();

                aimCoords.X = r.ReadSingle();
                aimCoords.Y = r.ReadSingle();
                aimCoords.Z = r.ReadSingle();

                data.AimCoords = aimCoords;
            }

            // Are we the driver?
            if (CheckBit(data.Flag.Value, VehicleDataFlags.Driver))
            {
                // Read position, rotation and velocity.
                Vector3 position = new Vector3();
                Vector3 rotation = new Vector3();
                Vector3 velocity = new Vector3();

                position.X = r.ReadSingle();
                position.Y = r.ReadSingle();
                position.Z = r.ReadSingle();

                rotation.X = r.ReadSingle();
                rotation.Y = r.ReadSingle();
                rotation.Z = r.ReadSingle();

                velocity.X = DecompressSingle(r.ReadUInt16());
                velocity.Y = DecompressSingle(r.ReadUInt16());
                velocity.Z = DecompressSingle(r.ReadUInt16());

                data.Position = position;
                data.Quaternion = rotation;
                data.Velocity = velocity;

                // Read car health
                data.VehicleHealth = r.ReadInt16();

                // read RPM & steering angle
                byte rpmCompressed = r.ReadByte();
                data.RPM = rpmCompressed/(float) byte.MaxValue;

                byte angleCompressed = r.ReadByte();
                var angleDenorm = 90f*(angleCompressed/(float) byte.MaxValue);
                data.Steering = angleDenorm - 45f;
            }

            return data;
        }

        public static VehicleData ReadLightVehicleSync(byte[] array)
        {
            var data = new VehicleData();
            var r = new BitReader(array);

            // Read player nethandle
            data.NetHandle = r.ReadInt32();
            
            // read model
            data.PedModelHash = r.ReadInt32();

            // read vehicle handle
            data.VehicleHandle = r.ReadInt32();

            // read vehicle seat
            data.VehicleSeat = (sbyte)r.ReadByte();

            // read gun model.
            data.WeaponHash = r.ReadInt32();

            // Does he have a traielr?
            if (r.ReadBoolean())
            {
                Vector3 trailerPos = new Vector3();

                trailerPos.X = r.ReadSingle();
                trailerPos.Y = r.ReadSingle();
                trailerPos.Z = r.ReadSingle();

                data.Trailer = trailerPos;
            }

            // Try to read latency
            if (r.CanRead(2))
            {
                var latency = r.ReadInt16();
                data.Latency = latency/1000f;
            }

            return data;
        }

        public static void ReadBasicSync(byte[] array, out int netHandle, out Vector3 position)
        {
            var r = new BitReader(array);

            // read netHandle
            netHandle = r.ReadInt32();

            // read position
            position = new Vector3();

            position.X = r.ReadSingle();
            position.Y = r.ReadSingle();
            position.Z = r.ReadSingle();
        }

        #endregion

        public static ushort CompressSingle(float value)
        {
            return (ushort) (value*256);
        }

        public static float DecompressSingle(ushort value)
        {
            return value/256f;
        }
        
        public static bool CheckBit(int value, VehicleDataFlags flag)
        {
            return (value & (int)flag) > 0;
        }

        public static bool CheckBit(int value, PedDataFlags flag)
        {
            return (value & (int)flag) > 0;
        }
    }
}