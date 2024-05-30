using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class MemberDTO
    {
       public int Id { get; set; }
    
        public string UserName { get; set; }

        public string PhotoUrl { get; set; }
    
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        public int Age { get; set; }

        public DateTime Created { get; set; } 

        public ICollection<PhotoDTO> Photos { get; set; }
    }
}