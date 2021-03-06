using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class TablaDePosiciones
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [BsonRequired]
    [BsonElement("IdTorneo")]
    public string IdTorneo { get; set; }

    [BsonRequired]
    [BsonElement("EquiposTablaPosicion")]
    public List<EquipoTablaPosicion> EquiposTablaPosicion { get; set; }

    public class EquipoTablaPosicion

    {

        public string idEquipo { get; set; }

        public int Puntos { get; set; }

        public int Posicion { get; set; }

        public int PG { get; set; }

        public int PP { get; set; }

        public int PF { get; set; }

        public int PC { get; set; }

        public int DIF { get; set; }
    }


}