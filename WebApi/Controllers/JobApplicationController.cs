using Microsoft.AspNetCore.Mvc;
using WebApi.Infrastructure;
using WebApi.Models;

namespace WebApi.Controllers;

public class JobApplicationController : Controller
{
    private readonly ILogger<JobApplicationController> _logger;
    private readonly Repository<Candidate> _repository;

    public JobApplicationController(
        ILogger<JobApplicationController> logger,
        Repository<Candidate> repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Candidate>> GetOrder(int id)
    {
        _logger.LogInformation(
            "Received request to get details for Candidate {id}",
            id);
        var order = await _repository.GetById(id);
        return order is null ? NotFound() : _mapper.Map<Order, OrderDto>(order);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        _logger.LogInformation(
            "Received request to get all the orders");
        var orders = _mapper
            .Map<List<Order>, List<OrderDto>>(
                await _repository.GetAsync());
        return orders;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> SubmitOrder(OrderDto orderDto)
    {
        _logger.LogInformation("Create Order with id {OrderId}", orderDto.Id);

        if (await OrderExistsAsync(orderDto.Id)) return BadRequest($"Order {orderDto.Id} already exists");

        var order = _mapper.Map<OrderDto, Order>(orderDto);
        order.CreatedDate = DateTimeOffset.UtcNow;
        order.LastUpdatedDate = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Created;
        await _repository.AddAsync(order);
        await _repository.SaveAsync();

        // Todo: Implement Transactional Outbox for this save and publish step
        _logger.LogInformation(
            "Publish an event for OrderSubmitted with OrderId {OrderId}",
            orderDto.Id);
        await _publishEndpoint.Publish<OrderCreated>(new
        {
            InVar.CorrelationId,
            OrderId = orderDto.Id,
            orderDto.CustomerId,
            orderDto.EventId,
            orderDto.TicketsCount
        });

        return CreatedAtAction(
            nameof(GetOrder),
            new { id = orderDto.Id },
            _mapper.Map<Order, OrderDto>(order));
    }

    [HttpPut]
    public async Task<ActionResult> UpdateOrder(OrderDto orderDto)
    {
        _logger.LogInformation("Update Order with id {OrderId}", orderDto.Id);

        if (orderDto.Id == Guid.Empty) return BadRequest("Order Id is required.");

        var order = await _repository.GetByIdAsync(orderDto.Id);

        if (order is null) return NotFound();

        _mapper.Map(orderDto, order);
        order.Status = OrderStatus.Updated;
        order.LastUpdatedDate = DateTimeOffset.UtcNow;
            
        try
        {
            _repository.Update(order);
            await _repository.SaveAsync();

            // Todo: Implement Transactional Outbox for this save and publish step
            _logger.LogInformation(
                "Publish an event for OrderUpdated with OrderId {OrderId}",
                orderDto.Id);
            await _publishEndpoint.Publish<OrderUpdated>(new
            {
                InVar.CorrelationId,
                OrderId = orderDto.Id,
                orderDto.CustomerId,
                orderDto.EventId,
                orderDto.TicketsCount
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await OrderExistsAsync(orderDto.Id))
                return NotFound();
            throw;
        }

        return NoContent();
    }
    
    [HttpPut("Complete")]
    public async Task<ActionResult> CompleteOrder(OrderDto orderDto)
    {
        _logger.LogInformation("Complete Order with id {OrderId}", orderDto.Id);

        if (orderDto.Id == Guid.Empty) return BadRequest("Order Id is required.");

        var order = await _repository.GetByIdAsync(orderDto.Id);

        if (order is null) return NotFound();
        
        order.CompletedDate = DateTimeOffset.UtcNow;
        
        try
        {
            _repository.Update(order);
            await _repository.SaveAsync();

            // Todo: Implement Transactional Outbox for this save and publish step
            _logger.LogInformation(
                "Publish an event for OrderCompleted with OrderId {OrderId}",
                orderDto.Id);
            await _publishEndpoint.Publish<OrderCompleted>(new
            {
                InVar.CorrelationId,
                OrderId = order.Id,
                order.CompletedDate
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await OrderExistsAsync(orderDto.Id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteOrder(Guid id)
    {
        _logger.LogInformation("Delete Order with id {OrderId}", id);

        if (id == Guid.Empty) return BadRequest("Order Id is required.");

        var order = await _repository.GetByIdAsync(id);

        if (order is null) return NotFound();

        try
        {
            await _repository.DeleteAsync(id);
            await _repository.SaveAsync();

            // Todo: Implement Transactional Outbox for this save and publish step
            _logger.LogInformation(
                "Publish an event for OrderCancelled with OrderId {OrderId}",
                id);
            await _publishEndpoint.Publish<OrderCancelled>(new
            {
                InVar.CorrelationId,
                OrderId = id,
                Reason = "Changed my mind"
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await OrderExistsAsync(id))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    private async Task<bool> OrderExistsAsync(Guid id)
    {
        var order = await _repository.GetByIdAsync(id);
        return order?.Id is not nul
}