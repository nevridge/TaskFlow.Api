import { type FormEvent, useState } from 'react'
import type { NoteResponseDto, CreateNoteDto } from '@/api/client/types.gen'

interface Props {
  note?: NoteResponseDto
  onSubmit: (data: CreateNoteDto) => void
  onCancel: () => void
}

export function NoteForm({ note, onSubmit, onCancel }: Props) {
  const [content, setContent] = useState(note?.content ?? '')

  function handleSubmit(e: FormEvent) {
    e.preventDefault()
    onSubmit({ content })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-3">
      <div className="flex flex-col gap-1">
        <label htmlFor="content" className="text-sm font-medium text-slate-700">Content</label>
        <textarea
          id="content"
          value={content}
          onChange={e => setContent(e.target.value)}
          rows={4}
          required
          className="rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
        />
      </div>
      <div className="flex justify-end gap-2">
        <button
          type="button"
          onClick={onCancel}
          className="rounded px-4 py-2 text-sm text-slate-600 hover:bg-slate-100"
        >
          Cancel
        </button>
        <button
          type="submit"
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          Save
        </button>
      </div>
    </form>
  )
}
