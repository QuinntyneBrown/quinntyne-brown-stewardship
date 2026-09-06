using System.Text.Json;
using QuinntyneBrownStewardship.Application.Programme;
using QuinntyneBrownStewardship.Domain.Notes;

namespace QuinntyneBrownStewardship.Application.Notes;

public static class NotePagination
{
    public const int PageSize = 20;
    private const int PageBytes = 16000;

    public static NotePageResponse Page(List<Note> candidates, Func<Note, NoteResponse> describe)
    {
        var page = new List<NoteResponse>();
        var bytes = 0;
        foreach (var note in candidates)
        {
            var response = describe(note);
            var size = JsonSerializer.SerializeToUtf8Bytes(response).Length;
            // A single valid note is always returned whole, even when its Unicode
            // JSON encoding exceeds the target. No body or revision is truncated.
            if (page.Count > 0 && (page.Count == PageSize || bytes + size > PageBytes)) break;
            page.Add(response); bytes += size;
        }
        var last = page.LastOrDefault();
        return new(page, page.Count < candidates.Count && last != null ? new NoteCursor(last.RevisedAt, last.Id).Encode() : null);
    }
}
