using System;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class GameToCreateDto
    {
        public Guid PlayerId { get; set; }

        [Range(1, 20)]
        public int TurnsCount { get; set; }
    }
}