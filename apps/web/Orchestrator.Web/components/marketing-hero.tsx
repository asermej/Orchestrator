import { Button } from "@/components/ui/button";
import { ArrowRight, Sparkles } from "lucide-react";

export function MarketingHero() {
  const scrollToPersonas = () => {
    const personasSection = document.getElementById("personas-section");
    personasSection?.scrollIntoView({ behavior: "smooth" });
  };

  return (
    <div className="relative overflow-hidden bg-gradient-to-br from-primary/10 via-background to-primary/5">
      {/* Background decoration */}
      <div className="absolute inset-0 bg-grid-white/10 bg-[size:20px_20px] [mask-image:radial-gradient(white,transparent_85%)]" />
      
      <div className="container mx-auto px-4 py-20 md:py-32 relative">
        <div className="max-w-4xl mx-auto text-center space-y-8">
          {/* Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-primary/10 border border-primary/20">
            <Sparkles className="h-4 w-4 text-primary" />
            <span className="text-sm font-medium">AI-Powered Conversations</span>
          </div>

          {/* Headline */}
          <h1 className="text-4xl md:text-6xl lg:text-7xl font-bold tracking-tight">
            Chat with AI Personas
            <br />
            <span className="text-primary">Tailored to Your Needs</span>
          </h1>

          {/* Subheadline */}
          <p className="text-xl md:text-2xl text-muted-foreground max-w-2xl mx-auto">
            Connect with specialized AI personalities across every field. Get expert advice, creative inspiration, and personalized insights.
          </p>

          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center items-center pt-4">
            <Button size="lg" asChild className="text-lg px-8 h-14">
              <a href="/api/auth/signup">
                Get Started Free
                <ArrowRight className="ml-2 h-5 w-5" />
              </a>
            </Button>
            <Button 
              size="lg" 
              variant="outline" 
              className="text-lg px-8 h-14"
              onClick={scrollToPersonas}
            >
              Browse Personas
            </Button>
          </div>

          {/* Social Proof */}
          <div className="flex flex-wrap justify-center gap-8 pt-8 text-sm text-muted-foreground">
            <div className="text-center">
              <div className="text-2xl font-bold text-foreground">1000+</div>
              <div>Active Personas</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-foreground">50K+</div>
              <div>Conversations</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-foreground">20+</div>
              <div>Categories</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

