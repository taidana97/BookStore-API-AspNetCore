using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookStore_API.Contracts;
using BookStore_API.Data;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore_API.Controllers
{
    /// <summary>
    /// Interacts with the Book Table
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILoggerService _logger;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _env;

        public BooksController(
            IBookRepository bookRepository,
            ILoggerService logger,
            IMapper mapper,
            IWebHostEnvironment env
            )
        {
            _bookRepository = bookRepository;
            _logger = logger;
            _mapper = mapper;
            _env = env;
        }

        private string GetImagePath(string fileName) => $"{_env.ContentRootPath}\\uploads\\{fileName}";

        /// <summary>
        /// Get All Books
        /// </summary>
        /// <returns>A List of Books</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBooks()
        {

            var location = GetCollerActionNames();

            try
            {
                _logger.LogInfo($"{location}: Attemted Call");

                var books = await _bookRepository.FindAll();
                var res = _mapper.Map<IList<BookDTO>>(books);

                foreach (var item in res)
                {
                    if(!string.IsNullOrEmpty(item.Image))
                    {
                        var imgPath = GetImagePath(item.Image);

                        if (System.IO.File.Exists(GetImagePath(item.Image)))
                        {
                            byte[] imgBytes = System.IO.File.ReadAllBytes(imgPath);
                            item.File = Convert.ToBase64String(imgBytes);
                        }
                    }
                }

                _logger.LogInfo($"{location}: Successfully got all Records");

                return Ok(res);
            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Gets a Book By Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A Book Record</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBook(int id)
        {
            var location = GetCollerActionNames();

            try
            {
                _logger.LogInfo($"{location}: Attempted Call for id: {id}");

                var book = await _bookRepository.FindById(id);

                if (book == null)
                {
                    _logger.LogWarn($"{location}: Failed to retrieve record with for id: {id}");
                    return NotFound();
                }

                var res = _mapper.Map<BookDTO>(book);

                if (!string.IsNullOrEmpty(res.Image))
                {
                    var imgPath = GetImagePath(res.Image);
                   
                    if(System.IO.File.Exists(imgPath))
                    {
                        byte[] imgBytes = System.IO.File.ReadAllBytes(imgPath);
                        res.File = Convert.ToBase64String(imgBytes);
                    }
                }

                _logger.LogInfo($"{location}: Successfully got record with id: {id}");

                return Ok(res);
            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Creates a new Book
        /// </summary>
        /// <param name="bookDTO"></param>
        /// <returns>Book Object</returns>
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Create([FromBody] BookCreateDTO bookDTO)
        {
            var location = GetCollerActionNames();

            try
            {
                _logger.LogInfo($"{location}: Create Attempted");

                if (bookDTO == null)
                {
                    _logger.LogWarn($"{location}: Empty Request was submitted");
                    return BadRequest(ModelState);
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"{location}: Data was Incomplete");
                    return BadRequest(ModelState);
                }

                var book = _mapper.Map<Book>(bookDTO);

                var isSuccess = await _bookRepository.Create(book);

                if (!isSuccess)
                {
                    return InternalError($"{location}: Creation failed");
                }

                if(!string.IsNullOrEmpty(bookDTO.File))
                {
                    var imgPath = GetImagePath(bookDTO.Image);
                    byte[] imgaeBytes = Convert.FromBase64String(bookDTO.File);

                    System.IO.File.WriteAllBytes(imgPath, imgaeBytes);
                }

                _logger.LogInfo($"{location}: Created was successful");
                _logger.LogInfo($"{location}: {book}");

                return Created("Create", new { book });
            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Updates An Book
        /// </summary>
        /// <param name="id"></param>
        /// <param name="bookDTO"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Update(int id, [FromBody] BookUpdateDTO bookDTO)
        {
            var location = GetCollerActionNames();

            try
            {
                _logger.LogInfo($"{location}: Update Attempted on record with id: {id}");

                if (id < 1 || bookDTO == null || id != bookDTO.Id)
                {
                    _logger.LogWarn($"{location}: Update failed with bad data - id: {id}");
                    return BadRequest();
                }

                var isExists = await _bookRepository.isExists(id);

                if (!isExists)
                {
                    _logger.LogWarn($"{location}: Failed to retrieve record with id: {id}");
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarn($"{location}: Data was Incomplete");
                    return BadRequest(ModelState);
                }

                var oldImage = await _bookRepository.GetImageFileImage(id);

                var book = _mapper.Map<Book>(bookDTO);

                var isSuccess = await _bookRepository.Update(book);

                if (!isSuccess)
                {
                    return InternalError($"{location}: Update failed");
                }

                if(!bookDTO.Image.Equals(oldImage))
                {
                    if (System.IO.File.Exists(GetImagePath(oldImage)))
                    {
                        System.IO.File.Delete(GetImagePath(oldImage));
                    }
                }

                if (!string.IsNullOrEmpty(bookDTO.File))
                {
                    byte[] imgaeBytes = Convert.FromBase64String(bookDTO.File);
                    System.IO.File.WriteAllBytes(GetImagePath(bookDTO.Image), imgaeBytes);
                }

                _logger.LogInfo($"{location}: Record with id: {id} successfully updated");
                return NoContent();
            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        /// <summary>
        /// Removes an book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(int id)
        {
            var location = GetCollerActionNames();

            try
            {
                _logger.LogInfo($"{location}: Delete Attempted on record with id: {id}");

                if (id < 1)
                {
                    _logger.LogWarn($"{location}: Delete failed with bad data - id: {id}");
                    return BadRequest();
                }

                var isExists = await _bookRepository.isExists(id);

                if (!isExists)
                {
                    _logger.LogWarn($"{location}: Failed to retrieve record with id: {id}");
                    return NotFound();
                }

                var oldImage = await _bookRepository.GetImageFileImage(id);

                var book = await _bookRepository.FindById(id);

                if (book == null)
                {
                    _logger.LogWarn($"{location}: Failed with id:{id} was not found");
                    return NotFound();
                }

                var isSuccess = await _bookRepository.Delete(book);

                if (!isSuccess)
                {
                    return InternalError($"{location}: Delete failed for record with id: {id}");
                }

                if (!string.IsNullOrEmpty(book.Image))
                {
                    if (System.IO.File.Exists(GetImagePath(oldImage)))
                    {
                        System.IO.File.Delete(GetImagePath(oldImage));
                    }
                }

                _logger.LogInfo($"{location}: Record with id: {id} successfully deleted");
                return NoContent();
            }
            catch (Exception e)
            {
                return InternalError($"{location}: {e.Message} - {e.InnerException}");
            }
        }

        private string GetCollerActionNames()
        {
            var controllers = ControllerContext.ActionDescriptor.ControllerName;
            var action = ControllerContext.ActionDescriptor.ActionName;

            return $"{controllers} - {action}";
        }

        private ObjectResult InternalError(string message)
        {
            _logger.LogError(message);
            return StatusCode(500, "Something went wrong. Please contact the Administrator");
        }
    }
}
