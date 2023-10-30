using Newtonsoft.Json;
using SimpleRelm.Attributes;
using SimpleRelm.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRelm.Models
{
    public class RelmModelApartment : RelmModel, IRelmModelApartment
    {
        [RelmColumn]
        public string ApartmentId { get; set; }

        [RelmColumn]
        public int UserId { get; set; }
        [RelmColumn]
        public string UserEmail { get; set; }
        [RelmColumn]
        public string UserName { get; set; }

        [RelmColumn]
        public IRelmMember Member { get; set; }

        public RelmModelApartment() : base() { }
        public RelmModelApartment(DataRow ModelData, string AlternateTableName = null) : base(ModelData, AlternateTableName: AlternateTableName) { }
    }
}
