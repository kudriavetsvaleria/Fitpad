using System;

namespace Fitpad.Model
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int Height { get; set; }
        public double Weight { get; set; }
        public DateTime BirthDate { get; set; }

        public UserModel() { }

        public UserModel(string username, string email, string password, int height, double weight, DateTime birthDate)
        {
            Username = username;
            Email = email;
            Password = password;
            Height = height;
            Weight = weight;
            BirthDate = birthDate;
        }
    }
}
