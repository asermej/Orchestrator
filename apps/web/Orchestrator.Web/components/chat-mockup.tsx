import { Card } from "@/components/ui/card";
import { PersonaAvatar } from "@/components/persona-avatar";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";

interface Message {
  role: "user" | "assistant";
  content: string;
}

interface ChatMockupProps {
  personaName: string;
  personaImage?: string;
  messages: Message[];
  title: string;
}

export function ChatMockup({
  personaName,
  personaImage,
  messages,
  title,
}: ChatMockupProps) {
  return (
    <Card className="overflow-hidden">
      {/* Header */}
      <div className="bg-muted/30 border-b px-4 py-3">
        <div className="flex items-center gap-3">
          <PersonaAvatar
            imageUrl={personaImage}
            displayName={personaName}
            size="sm"
            shape="circle"
          />
          <div>
            <h3 className="font-semibold text-sm">{personaName}</h3>
            <p className="text-xs text-muted-foreground">{title}</p>
          </div>
        </div>
      </div>

      {/* Messages */}
      <div className="p-4 space-y-4 bg-gradient-to-b from-background to-muted/10">
        {messages.map((message, index) => (
          <div
            key={index}
            className={`flex gap-3 ${
              message.role === "user" ? "justify-end" : "justify-start"
            }`}
          >
            {message.role === "assistant" && (
              <div className="flex-shrink-0">
                <PersonaAvatar
                  imageUrl={personaImage}
                  displayName={personaName}
                  size="sm"
                  shape="circle"
                />
              </div>
            )}
            
            <div
              className={`max-w-[80%] rounded-lg px-4 py-2.5 shadow-sm ${
                message.role === "user"
                  ? "bg-primary text-primary-foreground"
                  : "bg-muted border"
              }`}
            >
              <p className="text-sm leading-relaxed whitespace-pre-line">
                {message.content}
              </p>
            </div>

            {message.role === "user" && (
              <div className="flex-shrink-0">
                <Avatar className="h-8 w-8">
                  <AvatarFallback className="bg-primary/10 text-primary">
                    U
                  </AvatarFallback>
                </Avatar>
              </div>
            )}
          </div>
        ))}
      </div>
    </Card>
  );
}

