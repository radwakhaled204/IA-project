using jobconnect.Data;
using jobconnect.Dtos;
using jobconnect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace jobconnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommunicationController : ControllerBase
    {
        private readonly IDataRepository<Communication> _communicationRepository;
        private readonly IDataRepository<Messages> _messageRepository;
        private readonly IDataRepository<JobSeeker> _jobSeekerRepository;
        private readonly IDataRepository<Employer> _employerRepository;
        private readonly DataContext _db;


        public CommunicationController(IDataRepository<Communication> communicationRepository, 
            IDataRepository<Messages> messageRepository,
            IDataRepository<JobSeeker> jobSeekerRepository,
            IDataRepository<Employer> employerRepository, DataContext db)
        {
            _communicationRepository = communicationRepository;
            _jobSeekerRepository = jobSeekerRepository;
            _messageRepository = messageRepository;
            _employerRepository = employerRepository;
            _db = db;

        }
        /*********************************************************** CreateCommunication **********************************************************/

        [HttpPost]
        public async Task<IActionResult> CreateCommunication([FromBody] CommunicationDto communicationDto)
        {
            // check if both JobSeekerId and EmployerId exist
            var jobSeeker = await _jobSeekerRepository.GetByIdAsync(communicationDto.JobSeekerId);
            if (jobSeeker == null)
            {
                return BadRequest("Invalid JobSeekerId");
            }

            var employer = await _employerRepository.GetByIdAsync(communicationDto.EmployerId);
            if (employer == null)
            {
                return BadRequest("Invalid EmployerId");
            }

            // create communication 
            var communication = new Communication
            {
                JobSeekerId = communicationDto.JobSeekerId,
                EmployerId = communicationDto.EmployerId
            };

            // save communication to database
            await _communicationRepository.AddAsync(communication);
            await _communicationRepository.Save();

            return Ok(new { communication.CommunicationId });
        }

        /*********************************************************** message **********************************************************/


        [HttpPost("{communicationId}/message")]
        public async Task<IActionResult> SendMessage(int communicationId, [FromBody] MessageDto messageDto)
        {
            // check if communication exists
            var communication = await _communicationRepository.GetByIdAsync(communicationId);
            if (communication == null)
            {
                return BadRequest("Invalid CommunicationId");
            }

          
            var toId = messageDto.ToId;
            var fromId = messageDto.FromId;
            var toUser = await _db.User.FirstOrDefaultAsync(u => u.UserId == toId);
            var fromUser = await _db.User.FirstOrDefaultAsync(u => u.UserId == fromId);

            if (toUser == null || fromUser == null)
            {
                return BadRequest("Invalid ToId or FromId");
            }

           
            var message = new Messages
            {
                CommunicationId = communicationId,
                Content = messageDto.Content,
                ToId = messageDto.ToId,
                FromId = messageDto.FromId
            };

            message.Message_date = DateTime.Now;
            // save message to database
            await _messageRepository.AddAsync(message);
            await _messageRepository.Save();

            return Ok(new { message.MessageId });
        }

        /*********************************************************** Getmessage **********************************************************/

        [HttpGet("{communicationId}/message")]
        public async Task<IActionResult> GetMessages(int communicationId)
        {
           
            var communication = await _communicationRepository.GetByIdAsync(communicationId);
            if (communication == null)
            {
                return BadRequest("Invalid CommunicationId");
            }

   
            var messages = await _db.Messages
                .Where(m => m.CommunicationId == communicationId)
                .Select(m => new
                {
                    m.MessageId,
                    m.CommunicationId,
                    m.Communication.JobSeekerId,
                    m.Communication.EmployerId,
                    m.FromId,
                    m.ToId,
                    m.Content,
                    m.Message_date,
                })
                .ToListAsync();

            return Ok(messages);
        }



    }
}
