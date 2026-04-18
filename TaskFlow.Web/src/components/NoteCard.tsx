import { formatDate } from '@/lib/utils'
import type { NoteResponseDto } from '@/api/client/types.gen'

interface Props {
  note: NoteResponseDto
  onEdit: (note: NoteResponseDto) => void
  onDelete: (note: NoteResponseDto) => void
}

export function NoteCard({ note, onEdit, onDelete }: Props) {
  return (
    <div className="rounded-lg border bg-white p-3 flex flex-col gap-2">
      <p className="text-sm text-slate-800 whitespace-pre-wrap">{note.content}</p>
      <div className="flex items-center justify-between">
        <span className="text-xs text-slate-400">{formatDate(note.createdAt)}</span>
        <div className="flex gap-1">
          <button
            aria-label="Edit"
            onClick={() => onEdit(note)}
            className="rounded px-2 py-1 text-xs text-slate-600 hover:bg-slate-100"
          >
            Edit
          </button>
          <button
            aria-label="Delete"
            onClick={() => onDelete(note)}
            className="rounded px-2 py-1 text-xs text-red-600 hover:bg-red-50"
          >
            Delete
          </button>
        </div>
      </div>
    </div>
  )
}
