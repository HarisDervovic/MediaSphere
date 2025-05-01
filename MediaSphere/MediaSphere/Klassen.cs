using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaSphere
{
    public class Medium
    {
        public int MedienID { get; set; }
        public string Pfad { get; set; }
        public string Typ { get; set; }
        public string Titel { get; set; }
        public string Kategorie { get; set; }
    }

    public class Playlist
    {
        public int PlaylistID { get; set; }
        public int BenutzerID { get; set; }
        public string Name { get; set; }
        public DateTime Erstellungsdatum { get; set; }


        public override string ToString()
        {
            return Name;
        }
    }

    public class PlaylistMedium
    {
        public int PlaylistID { get; set; }
        public int MedienID { get; set; }
        public int Reihenfolge { get; set; }

        // Optionale Navigationseigenschaft, wenn du Medien direkt abrufst
        //public Medium Medium { get; set; }
    }
}
