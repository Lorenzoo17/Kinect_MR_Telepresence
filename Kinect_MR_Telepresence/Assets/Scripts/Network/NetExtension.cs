using UnityEngine;
using LiteNetLib.Utils;
using Intel.RealSense;

namespace Extension{ //Per utilizzare queste estensioni devo importare il namespace Extension
    public static class NetExtension{

        public static void Put(this NetDataWriter writer, Vector3 vector){ //Definizione di metodo extension, con this vado a specificare il tipo di dato a cui il metodo fa riferimento, in questo modo facendo writer.Put(vector) --> posso direttamente utilizzare questo metodo, come se facesse parte della classe NetDataWriter 
            writer.Put(vector.x);
            writer.Put(vector.y);
            writer.Put(vector.z);
        }

        public static void Put(this NetDataWriter writer, Vector2 vector) { //Definizione di metodo extension, con this vado a specificare il tipo di dato a cui il metodo fa riferimento, in questo modo facendo writer.Put(vector) --> posso direttamente utilizzare questo metodo, come se facesse parte della classe NetDataWriter 
            writer.Put(vector.x);
            writer.Put(vector.y);
        }

        public static void Put(this NetDataWriter writer, Quaternion rotation){ //Per trasmettere quaternioni per rotazioni
            writer.Put(rotation.x);
            writer.Put(rotation.y);
            writer.Put(rotation.z);
            writer.Put(rotation.w);
        }
        public static void Put(this NetDataWriter writer, Intrinsics intrinsics) { //Per trasmettere intrinisc in rete
            writer.Put(intrinsics.width);
            writer.Put(intrinsics.height);
            writer.Put(intrinsics.fx);
            writer.Put(intrinsics.fx);
            writer.Put(intrinsics.ppx);
            writer.Put(intrinsics.ppy);
        }

        public static Vector3 GetVector3(this NetDataReader reader){
            Vector3 vector;
            vector.x = reader.GetFloat();
            vector.y = reader.GetFloat();
            vector.z = reader.GetFloat();

            return vector;
        }

        public static Vector3 GetVector2(this NetDataReader reader) {
            Vector2 vector;
            vector.x = reader.GetFloat();
            vector.y = reader.GetFloat();

            return vector;
        }

        public static Quaternion GetQuaternion(this NetDataReader reader){
            Quaternion rotation;
            rotation.x = reader.GetFloat();
            rotation.y = reader.GetFloat();
            rotation.z = reader.GetFloat();
            rotation.w = reader.GetFloat();

            return rotation;
        }

        public static Intrinsics GetIntrinsics(this NetDataReader reader) {
            Intrinsics intrinsics = new Intrinsics();

            intrinsics.width = reader.GetInt();
            intrinsics.height = reader.GetInt();
            intrinsics.fx = reader.GetFloat();
            intrinsics.fy = reader.GetFloat();
            intrinsics.ppx = reader.GetFloat();
            intrinsics.ppy = reader.GetFloat();

            return intrinsics;
        }
    }
}