# SimpleDecoder
Simple mp3 decoder. Simple here means using derivative works to learn and understand how to build an mp3 decoder. Purpose of which is to also write understandable code that is more verbose so that I give back to the community more easier to understand code.  This is for educational purpose for others and myself but can be used in any creative ways.

## IS this used, where is examples on how to use it?
This library is used in my own audio project built around learning audio decoders.
It is called **Digital Audio Experiment** and can be found on my repository here: [Digital Audio Experiment](https://github.com/chandmm/DigitalAudioExperiment)

URL: https://github.com/chandmm/DigitalAudioExperiment

## Why did I do it?
I wanted to see if I can, not because I should. I reached a point in my career where I should be able to tackle complex problems and so what could be the first hard step in a personal project than to build a complex decoder such as Mpeg 1 Layer III audio library. 

### What else?
Actually more that this is the primary reason equal to "Why did I do it". I wanted to see if AI could become an effective tool to help comples problems. My choice was to use ChatGPT from open AI. 
**How did it fair off?**
Originally I wanted to make exclusive use of the AI and work with it to build the decoder. However it looks like ChatGPT hit its limit fairly quickly. A few weeks was spent prompt engineering in order to get it to answer complex questions. Even breaking the complexity into smaller and smaller problem domains. In the end, I couldnt do without using others works in unisen. There are still much I dont understand and had to use direct copy of the code. That learning is still work in progress. 

## Specifications/details
- This decoder only decodes MPEG 1 Layer 3 files. The intention was not to make a fully working mp3 decoder but learn how modern compressions algoritms for audio work along with playing audio electronically.
- This decoder can read VBR mp3's even though the source code does not specifically look for XING headers. IT does this by reading each frame and decoding based on its header information about the frame. Eg frame size from its bitrate. As souce it implicitely handles VBR, and not specifically.
- There are issues. I made the buffer much too simple so some mp3's do fail to play correctly. Thought in my testing, there arent very many mp3's that this decoder fails on. In saying that, failing on just one is enough to say this isnt 100% compatable with all mp3's.
- Standards: I ignored the maximum buffer limits. It was much simpler to write code that reads the whole frame, remove as needed and once the current frame is done, any left over data is in place as bit reservoir.
- I wasnt aiming for performance, just readibility.
- I have tried to keep as closely to the huffman tables described in the standards document as possible. This means I produced a json file that embodies those tables data. Inside the code I then use a tree graph algorithm to do the huffman code search which significantly boosted performance.

## Notes:
Why is the bitreservoir code so simple?
**SimpleDeocder** :) There are plenty of open source mp3 decoders far better than this. I only wanted to learn and then to provide code that is easier to follow for others. This part is still work in progress and will be for a while so code will get updated, architecure changes will occur as I learn more about this. Also From research, while it will be hard to uproot mp3, other formats exist that are superior such as flac and will eventually replace mp3's. I cannot be held to that, this is just my opinion from gathered knowledge over the development period.

So that means other layers may be added later, or maybe the community can help as long as the code remains simple. That means descriptive variable names instead of single letter names, and descriptive method names.

## Conclusion
AI is an excellent tool as an an assistant, and it fairs well when problems are borken down into smaller problem domain while using additional resources to verify. In sshort, even when its wrong, it provides a good lead to whaty resources to look for.

# Final words:
I hope this is useful.

### TODO and Notes:
Desciptions on how to use the decoder libraries.


### Citations and derivative works
**This code makes use of the following for learning and adaptating code:**
- Mp3 standards document ISO/IEC 11172-3
- ISO/IEC 13818-3
- Mp3Sharp source code by "The Authors"
- Javazoom source code.

## For analysis of code, understanding documents, algorithms and generally asking questions
- ChatGPT by OpenAI