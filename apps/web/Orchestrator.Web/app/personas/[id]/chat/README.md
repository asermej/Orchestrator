# Chat Integration

This directory contains the chat interface for conversations with personas, integrating with the ChatMessage and PersonaChat backend endpoints.

## Features

- **Automatic Chat Creation**: When a user first visits a persona's chat page, a new chat is automatically created
- **Past Conversations**: Users can view and switch between past conversations in the sidebar
- **Real-time Messaging**: Messages are sent to the backend and AI responses are received
- **Responsive Design**: Mobile-friendly with a collapsible sidebar
- **Message History**: All messages are persisted and loaded from the backend

## Components

### Page (`page.tsx`)
The main chat interface with:
- Sidebar showing past conversations
- Chat messages area
- Message input form
- Mobile-responsive layout

### Actions (`actions.ts`)
Server-side functions that interact with the API:
- `fetchPersonaById(id)` - Get persona details
- `createPersonaChat(personaId, userId, title?)` - Create a new chat
- `fetchPersonaChats(personaId, userId)` - Get all chats for a user/persona
- `fetchChatMessages(chatId)` - Get all messages in a chat
- `sendMessage(chatId, content)` - Send a message and get AI response
- `updateChatTitle(chatId, title)` - Update chat title

## API Endpoints Used

### PersonaChat Controller
- `POST /api/v1/personachat` - Create new chat
- `GET /api/v1/personachat?PersonaId={id}&UserId={id}` - Get user's chats
- `PUT /api/v1/personachat/{id}` - Update chat title
- `DELETE /api/v1/personachat/{id}` - Delete chat

### ChatMessage Controller
- `POST /api/v1/chatmessage/send` - Send message and get AI response
- `GET /api/v1/chatmessage?ChatId={id}` - Get all messages in chat
- `GET /api/v1/chatmessage/{id}` - Get specific message
- `DELETE /api/v1/chatmessage/{id}` - Delete message

## User Flow

1. User navigates to `/personas/{personaId}/chat`
2. System checks for existing chats for this user/persona
3. If chats exist, loads the most recent one
4. If no chats exist, creates a new one automatically
5. User can:
   - Send messages and receive AI responses
   - Switch between past conversations
   - Create new conversations
   - View message history

## Environment Variables

Add to your `.env.local`:

```bash
NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1
```

## Important Notes

### User ID Mapping
⚠️ **Important**: Auth0's `sub` claim is a string (e.g., "auth0|123"), but the backend expects a `Guid` for `UserId`.

**Current Implementation**: The Auth0 sub is passed directly as the userId. This will work if:
- Your backend accepts string user IDs
- You have middleware to convert Auth0 subs to Guids
- You've created User records with the Auth0 sub as the Guid

**Production Recommendation**: Implement one of these solutions:
1. Create a User record on first login with a Guid, store Auth0 sub as a separate field
2. Create a mapping table between Auth0 subs and User Guids
3. Modify the backend to use string user IDs instead of Guids

### URL Parameters
- `?chatId={id}` - Loads a specific chat (optional)
- If no chatId is provided, loads the most recent chat or creates a new one

## Styling

Uses Tailwind CSS with shadcn/ui components:
- `ScrollArea` - For scrollable conversation list
- `Avatar` - User and persona avatars
- `Card` - Message containers
- `Button` - Actions and navigation
- `Input` - Message input field

## Mobile Support

- Sidebar is hidden by default on mobile
- Toggle button to show/hide conversations
- Always visible on desktop (md breakpoint)

