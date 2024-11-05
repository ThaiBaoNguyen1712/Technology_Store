namespace Tech_Store.Models.DTO
{
    public class Ward
    {
        public int Code { get; set; }
        public string Name { get; set; }
    }

    public class District
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public List<Ward> Wards { get; set; }
    }

    public class Province
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public List<District> Districts { get; set; }
    }
}
