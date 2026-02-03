import { Card, CardContent } from "@/components/ui/card";
import { Search, MessageCircle, TrendingUp } from "lucide-react";

export function HowItWorks() {
  const steps = [
    {
      number: "01",
      icon: Search,
      title: "Choose Your Agent",
      description: "Browse experts across categories—from business advisors to creative minds, tech gurus to wellness guides.",
    },
    {
      number: "02",
      icon: MessageCircle,
      title: "Start Chatting",
      description: "Engage in intelligent, natural conversations. Ask questions, get advice, or collaborate on ideas.",
    },
    {
      number: "03",
      icon: TrendingUp,
      title: "Learn & Grow",
      description: "Get personalized insights, expert knowledge, and actionable advice tailored to your needs.",
    },
  ];

  return (
    <div className="bg-muted/30 py-20">
      <div className="container mx-auto px-4">
        <div className="max-w-6xl mx-auto">
          {/* Section Header */}
          <div className="text-center mb-16">
            <h2 className="text-3xl md:text-4xl font-bold mb-4">
              How It Works
            </h2>
            <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
              Get started with AI-powered conversations in three simple steps
            </p>
          </div>

          {/* Steps Grid */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {steps.map((step, index) => (
              <Card key={index} className="relative overflow-hidden border-2 hover:border-primary/50 transition-all">
                {/* Step Number Background */}
                <div className="absolute top-4 right-4 text-6xl font-bold text-primary/5">
                  {step.number}
                </div>

                <CardContent className="p-6 space-y-4 relative">
                  {/* Icon */}
                  <div className="inline-flex p-3 rounded-lg bg-primary/10">
                    <step.icon className="h-6 w-6 text-primary" />
                  </div>

                  {/* Title */}
                  <h3 className="text-xl font-semibold">
                    {step.title}
                  </h3>

                  {/* Description */}
                  <p className="text-muted-foreground">
                    {step.description}
                  </p>
                </CardContent>

                {/* Connecting Arrow (hidden on last item and mobile) */}
                {index < steps.length - 1 && (
                  <div className="hidden md:block absolute top-1/2 -right-4 transform -translate-y-1/2 z-10">
                    <div className="w-8 h-0.5 bg-primary/30" />
                  </div>
                )}
              </Card>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

