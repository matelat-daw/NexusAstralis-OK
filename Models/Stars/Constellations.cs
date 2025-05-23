﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NexusAstralis.Models.User;

namespace NexusAstralis.Models.Stars;

public partial class Constellations
{
    [Key]
    public int id { get; set; }

    public string code { get; set; }

    public string latin_name { get; set; }

    public string english_name { get; set; }

    public string spanish_name { get; set; }

    public string mythology { get; set; }

    public double? area_degrees { get; set; }

    public string declination { get; set; }

    public string celestial_zone { get; set; }

    public string ecliptic_zone { get; set; }

    public string brightest_star { get; set; }

    public string discovery { get; set; }

    public string image_name { get; set; }

    public string image_url { get; set; }

    [ForeignKey("constellation_id")]
    [InverseProperty("constellation")]
    public virtual ICollection<Stars> star { get; set; } = new List<Stars>();
}