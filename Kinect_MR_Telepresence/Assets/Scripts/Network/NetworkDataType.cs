public enum NetworkDataType{
    PingPacket, //Pacchettu utilizzato per indicare messaggio di ping
    UserSpawnPacket, //Pacchetto utilizzato per indicare lo spawn del prefab dell'utente
    UserDespawnPacket,
    PositionPacket, //Pacchetto utilizzato per aggiornare la posizione di uno degli utenti
    PositionRotationPacket, //Pacchetto utilizzato per aggiornare la posizione e rotazione di uno degli utenti
    DepthColorPacket,
    CalibrationPacket,
    ItemStatePacket,
    ItemOwnership,
    AudioPacket,
    DepthColorRs, //Depth e color frame del realsense
    CalibrationRs //Parametri di calibrazione (intrinsics) del realSense
}