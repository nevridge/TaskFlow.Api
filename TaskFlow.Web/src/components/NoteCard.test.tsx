import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { NoteCard } from './NoteCard'
import type { NoteResponseDto } from '@/api/client/types.gen'

const note: NoteResponseDto = {
  id: 1,
  content: 'Remember to test this',
  taskItemId: 2,
  createdAt: '2026-04-17T10:00:00Z',
}

describe('NoteCard', () => {
  it('renders note content', () => {
    render(<NoteCard note={note} onEdit={vi.fn()} onDelete={vi.fn()} />)
    expect(screen.getByText('Remember to test this')).toBeInTheDocument()
  })

  it('calls onEdit when edit button is clicked', async () => {
    const onEdit = vi.fn()
    render(<NoteCard note={note} onEdit={onEdit} onDelete={vi.fn()} />)
    await userEvent.click(screen.getByRole('button', { name: /edit/i }))
    expect(onEdit).toHaveBeenCalledWith(note)
  })

  it('calls onDelete when delete button is clicked', async () => {
    const onDelete = vi.fn()
    render(<NoteCard note={note} onEdit={vi.fn()} onDelete={onDelete} />)
    await userEvent.click(screen.getByRole('button', { name: /delete/i }))
    expect(onDelete).toHaveBeenCalledWith(note)
  })
})
