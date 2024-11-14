namespace Nop.Plugin.Misc.Moloni.Models
{
    public class Document
    {
        public int document_id { get; set; }
        public int document_type_id { get; set; }
        public int document_set_id { get; set; }
        public string? document_set_name { get; set; }
        public int number { get; set; }
        public DateTime date { get; set; }
        public DocumentType? document_type { get; set; }
    }

    public class DocumentType
    {
        public int document_type_id { get; set; }
        public string? saft_code { get; set; }
        public string? titulo { get; set; }
    }

    public class AssociatedDocument
    {
        public int associated_id { get; set; }
        public float value { get; set; }
    }
}
