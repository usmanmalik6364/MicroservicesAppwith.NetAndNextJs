using System;
using System.Runtime.CompilerServices;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;


[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;

    public AuctionsController(AuctionDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
    {
        var auctions = await _context.Auctions
            .Include(x => x.Item)
            .OrderBy(y => y.Item.Make)
            .ToListAsync();
        return _mapper.Map<List<AuctionDto>>(auctions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        var auction = await _context.Auctions
            .Include(a => a.Item)
            .Where(a => a.Id == id)
            .Select(a => new AuctionDto
            {
                Id = a.Id,
                ReservePrice = a.ReservePrice,
                Seller = a.Seller,
                Winner = a.Winner,
                SoldAmount = a.SoldAmount,
                CurrentHighBid = a.CurrentHighBid,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                AuctionEnd = a.AuctionEnd,
                Status = a.Status.ToString(),

                // These come from the Item entity
                Make = a.Item.Make,
                Model = a.Item.Model,
                Year = a.Item.Year,
                Color = a.Item.Color,
                Mileage = a.Item.Mileage,
                ImageUrl = a.Item.ImageUrl
            })
            .SingleOrDefaultAsync();

        if (auction == null) return NotFound();

        return auction;
    }
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);
        //TODO add current user as seller 
        auction.Seller = "Test";
        _context.Auctions.Add(auction);

        var result = await _context.SaveChangesAsync() > 0;

        if (!result) return BadRequest("Could not save the changes to the DB");

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, _mapper.Map<AuctionDto>(auction));
    }
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _context.Auctions.Include(a => a.Item).FirstOrDefaultAsync(x => x.Id == id);
        if (auction == null) return NotFound();
        //TODO check seller == username when identity is implemented

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

        var result = await _context.SaveChangesAsync() > 0;
        if (!result) return BadRequest("Could not save to db");
        return Ok();

    }
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);
        if (auction == null) return NotFound();

        //TODO: check seller == username once identity is implemented
        _context.Auctions.Remove(auction);
        var result = await _context.SaveChangesAsync() > 0;
         if (!result) return BadRequest("Could not save to db");
        return Ok();
    }

}
